using System.Text.Json;
using System.Text.Json.Nodes;
using csce360ChrisExampleUI.Server.Mcp;
using ModelContextProtocol.Client;

namespace csce360ChrisExampleUI.Server.Services
{
    public record NaturalLanguageSearchResult(string Summary, IEnumerable<object> Products);

    public class NaturalLanguageSearchService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NaturalLanguageSearchService> _logger;
        private readonly McpClientProvider _mcpClientProvider;

        public NaturalLanguageSearchService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<NaturalLanguageSearchService> logger,
            McpClientProvider mcpClientProvider)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _mcpClientProvider = mcpClientProvider;
        }

        public async Task<NaturalLanguageSearchResult> RunAsync(string userQuery, CancellationToken ct = default)
        {
            var apiKey = _configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Configuration value 'Gemini:ApiKey' was not found.");
            var model = _configuration["Gemini:Model"] ?? "gemini-3.8-flash";

            var mcpClient = await _mcpClientProvider.GetClientAsync();

            var mcpTools = await mcpClient.ListToolsAsync(cancellationToken: ct);

            var functionDeclarations = new JsonArray(mcpTools.Select(t => (JsonNode)new JsonObject
            {
                ["name"] = t.Name,
                ["description"] = t.Description,
                ["parameters"] = ToGeminiSchema(JsonNode.Parse(t.JsonSchema.GetRawText())!)
            }).ToArray());

            var tools = new JsonArray
            {
                new JsonObject { ["functionDeclarations"] = functionDeclarations }
            };

            var contents = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["text"] = $"""
                                A user typed this product search in plain language: "{userQuery}"

                                Use the available tools to find matching products. If a category or company
                                name is mentioned, check it against list_categories / list_companies first if
                                you are not sure of the exact spelling. Once you have final results, reply with
                                ONE short plain-text sentence summarizing the search, with no further tool calls.
                                """
                        }
                    }
                }
            };

            var geminiClient = _httpClientFactory.CreateClient("Gemini");
            List<object> lastResults = new();
            const int maxTurns = 6;

            for (var turn = 0; turn < maxTurns; turn++)
            {
                var requestBody = new JsonObject
                {
                    ["contents"] = JsonNode.Parse(contents.ToJsonString()),
                    ["tools"] = JsonNode.Parse(tools.ToJsonString())
                };

                var url = $"v1beta/models/{model}:generateContent?key={apiKey}";
                var response = await geminiClient.PostAsJsonAsync(url, requestBody, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("Gemini API returned {StatusCode}: {Body}", response.StatusCode, errorBody);
                    throw new InvalidOperationException($"Gemini API request failed ({response.StatusCode}): {errorBody}");
                }

                var responseJson = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: ct)
                    ?? throw new InvalidOperationException("Empty response from Gemini API.");

                var candidate = responseJson["candidates"]?.AsArray().FirstOrDefault()
                    ?? throw new InvalidOperationException("Gemini returned no candidates.");

                var modelParts = candidate["content"]!["parts"]!.AsArray();

                var functionCalls = modelParts
                    .Where(p => p?["functionCall"] is not null)
                    .ToList();

                // Echo the model's turn back into the conversation history.
                contents.Add(new JsonObject
                {
                    ["role"] = "model",
                    ["parts"] = JsonNode.Parse(modelParts.ToJsonString())
                });

                if (functionCalls.Count == 0)
                {
                    var summary = modelParts
                        .Where(p => p?["text"] is not null)
                        .Select(p => p!["text"]!.GetValue<string>())
                        .FirstOrDefault() ?? "Here are the matching products.";

                    return new NaturalLanguageSearchResult(summary, lastResults);
                }

                var functionResponseParts = new JsonArray();

                foreach (var call in functionCalls)
                {
                    var functionCall = call!["functionCall"]!;
                    var toolName = functionCall["name"]!.GetValue<string>();
                    var argsNode = functionCall["args"] as JsonObject ?? new JsonObject();

                    var arguments = argsNode.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (object?)JsonSerializer.Deserialize<object>(kvp.Value!.ToJsonString()));

                    _logger.LogInformation("MCP tool call: {ToolName} {Args}", toolName, argsNode.ToJsonString());

                    var toolResult = await mcpClient.CallToolAsync(toolName, arguments, cancellationToken: ct);
                    var resultText = string.Join("\n", toolResult.Content
                        .Select(c => c.AsText())
                        .Where(t => t is not null));

                    if (toolName == "search_products")
                    {
                        try
                        {
                            lastResults = JsonSerializer.Deserialize<List<object>>(resultText) ?? lastResults;
                        }
                        catch (JsonException)
                        {
                            // leave lastResults as-is
                        }
                    }

                    // Gemini expects functionResponse.response to be a JSON object,
                    // so wrap raw tool output under a "result" key.
                    JsonNode responsePayload;
                    try
                    {
                        responsePayload = new JsonObject { ["result"] = JsonNode.Parse(resultText) };
                    }
                    catch (JsonException)
                    {
                        responsePayload = new JsonObject { ["result"] = resultText };
                    }

                    functionResponseParts.Add(new JsonObject
                    {
                        ["functionResponse"] = new JsonObject
                        {
                            ["name"] = toolName,
                            ["response"] = responsePayload
                        }
                    });
                }

                contents.Add(new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = functionResponseParts
                });
            }

            return new NaturalLanguageSearchResult(
                "I wasn't able to narrow this down in time — try rephrasing your search.",
                lastResults);
        }

        // Gemini's function-parameter schema is a restricted subset of OpenAPI 3.0.
        // Strip anything MCP's JSON Schema output includes that Gemini doesn't
        // understand (e.g. $schema, additionalProperties) and recurse into
        // nested object/array schemas.
        private static JsonNode ToGeminiSchema(JsonNode schema)
        {
            if (schema is not JsonObject obj)
            {
                return schema.DeepClone();
            }

            var allowedKeys = new[] { "type", "description", "enum", "items", "properties", "required", "format", "nullable" };
            var result = new JsonObject();

            foreach (var key in allowedKeys)
            {
                if (!obj.TryGetPropertyValue(key, out var value) || value is null)
                {
                    continue;
                }

                // Handle the "type" field specifically to prevent the list/array error
                if (key == "type")
                {
                    string? typeString = null;

                    if (value is JsonArray arr)
                    {
                        // If it's an array like ["string", "null"], take the first non-null type
                        typeString = arr.Select(x => x?.GetValue<string>()).FirstOrDefault(x => x != "null");

                        // If "null" was in the array, explicitly mark it as nullable for Gemini
                        if (arr.Any(x => x?.GetValue<string>() == "null"))
                        {
                            result["nullable"] = true;
                        }
                    }
                    else
                    {
                        typeString = value.GetValue<string>();
                    }

                    // Gemini expects uppercase type names (e.g., "STRING", "OBJECT", "ARRAY")
                    result["type"] = typeString?.ToUpperInvariant() ?? "OBJECT";
                    continue;
                }

                result[key] = key switch
                {
                    "items" => ToGeminiSchema(value),
                    "properties" => new JsonObject(
                        value.AsObject().Select(kvp =>
                            new KeyValuePair<string, JsonNode?>(kvp.Key, ToGeminiSchema(kvp.Value!)))),
                    _ => value.DeepClone()
                };
            }

            // Gemini requires "type" to be present; default to "OBJECT" if omitted
            if (!result.ContainsKey("type"))
            {
                result["type"] = "OBJECT";
            }

            return result;
        }
    }
}