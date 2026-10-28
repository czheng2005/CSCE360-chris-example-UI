using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using csce360ChrisExampleUI.Server.Mcp;
using csce360ChrisExampleUI.Server.Models;


namespace csce360ChrisExampleUI.Server.Services
{
    public record NaturalLanguageSearchResult(string Summary, IEnumerable<ProductResult> Products);

    // Runs a Gemini function-calling loop. Tool calls are dispatched straight to
    // the ProductTools methods (the same ones exposed over /mcp), so there is no
    // MCP client, loopback HTTP hop, or session lifecycle involved.
    public class NaturalLanguageSearchService
    {
        private const int MaxTurns = 6;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NaturalLanguageSearchService> _logger;
        private readonly ProductTools _tools;

        public NaturalLanguageSearchService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<NaturalLanguageSearchService> logger,
            ProductTools tools)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _tools = tools;
        }

        // Hand-written Gemini declarations mirroring ProductTools. Parameterless
        // tools deliberately omit "parameters" (Gemini rejects an OBJECT schema
        // with no properties).
        private static JsonArray BuildToolDeclarations() => JsonNode.Parse("""
        [
          {
            "functionDeclarations": [
              {
                "name": "search_products",
                "description": "Search products, optionally filtered by price range, category, on-sale status, and/or company name.",
                "parameters": {
                  "type": "OBJECT",
                  "properties": {
                    "minPrice":    { "type": "NUMBER",  "description": "Minimum price, inclusive. Omit for no lower bound." },
                    "maxPrice":    { "type": "NUMBER",  "description": "Maximum price, inclusive. Omit for no upper bound." },
                    "category":    { "type": "STRING",  "description": "Exact category name, e.g. 'Hardware'. Omit for all categories." },
                    "onSale":      { "type": "BOOLEAN", "description": "True = only on-sale items, false = only non-sale items, omit for both." },
                    "companyName": { "type": "STRING",  "description": "Exact company/vendor name. Omit for all companies." }
                  }
                }
              },
              {
                "name": "list_categories",
                "description": "List all distinct product category names. Use this to check valid values for search_products' category parameter before guessing."
              },
              {
                "name": "list_companies",
                "description": "List all distinct company/vendor names. Use this to check valid values for search_products' companyName parameter before guessing."
              }
            ]
          }
        ]
        """)!.AsArray();

        public async Task<NaturalLanguageSearchResult> RunAsync(string userQuery, CancellationToken ct = default)
        {
            var apiKey = _configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Configuration value 'Gemini:ApiKey' was not found.");
            var model = "gemini-3.5-flash-lite";

            var tools = BuildToolDeclarations();

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
            var lastResults = new List<ProductResult>();
            var url = $"v1beta/models/{model}:generateContent";

            for (var turn = 0; turn < MaxTurns; turn++)
            {
                var requestBody = new JsonObject
                {
                    // DeepClone: a JsonNode can only have one parent.
                    ["contents"] = contents.DeepClone(),
                    ["tools"] = tools.DeepClone()
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(requestBody)
                };
                request.Headers.Add("x-goog-api-key", apiKey);

                using var response = await geminiClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("Gemini API returned {StatusCode}: {Body}", response.StatusCode, errorBody);
                    throw new InvalidOperationException($"Gemini API request failed ({response.StatusCode}): {errorBody}");
                }

                var responseJson = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: ct)
                    ?? throw new InvalidOperationException("Empty response from Gemini API.");

                var candidate = responseJson["candidates"]?.AsArray().FirstOrDefault();
                var modelParts = candidate?["content"]?["parts"]?.AsArray();

                if (modelParts is null)
                {
                    _logger.LogWarning(
                        "Gemini returned no content. finishReason={Reason} promptFeedback={Feedback}",
                        candidate?["finishReason"], responseJson["promptFeedback"]);
                    return new NaturalLanguageSearchResult(
                        "The model didn't return an answer. Try rephrasing your search.", lastResults);
                }

                // Echo the model's turn back verbatim (preserves thought signatures).
                contents.Add(new JsonObject
                {
                    ["role"] = "model",
                    ["parts"] = modelParts.DeepClone()
                });

                var functionCalls = modelParts
                    .Where(p => p?["functionCall"] is not null)
                    .ToList();

                if (functionCalls.Count == 0)
                {
                    var summary = modelParts
                        .Select(p => p?["text"]?.GetValue<string>())
                        .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t))
                        ?? "Here are the matching products.";

                    return new NaturalLanguageSearchResult(summary, lastResults);
                }

                var functionResponseParts = new JsonArray();

                foreach (var call in functionCalls)
                {
                    var functionCall = call!["functionCall"]!;
                    var toolName = functionCall["name"]!.GetValue<string>();
                    var args = functionCall["args"] as JsonObject ?? new JsonObject();

                    _logger.LogInformation("Tool call: {ToolName} {Args}", toolName, args.ToJsonString());

                    JsonNode? payload;
                    try
                    {
                        object? result;
                        switch (toolName)
                        {
                            case "search_products":
                                var products = (await _tools.SearchProducts(
                                    GetDecimal(args, "minPrice"),
                                    GetDecimal(args, "maxPrice"),
                                    GetString(args, "category"),
                                    GetBool(args, "onSale"),
                                    GetString(args, "companyName"))).ToList();
                                lastResults = products;
                                result = products;
                                break;

                            case "list_categories":
                                result = await _tools.ListCategories();
                                break;

                            case "list_companies":
                                result = await _tools.ListCompanies();
                                break;

                            default:
                                result = new { error = $"Unknown tool '{toolName}'." };
                                break;
                        }

                        payload = new JsonObject { ["result"] = JsonSerializer.SerializeToNode(result) };
                    }
                    catch (Exception ex) when (ex is HttpRequestException or JsonException)
                    {
                        // Surface tool failures to the model instead of failing the whole request.
                        _logger.LogError(ex, "Tool {ToolName} failed", toolName);
                        payload = new JsonObject { ["error"] = ex.Message };
                    }

                    functionResponseParts.Add(new JsonObject
                    {
                        ["functionResponse"] = new JsonObject
                        {
                            ["name"] = toolName,
                            ["response"] = payload
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
                "I wasn't able to narrow this down in time. Try rephrasing your search.",
                lastResults);
        }

        // --- Argument helpers: tolerate missing keys, explicit nulls, wrong types ---

        private static decimal? GetDecimal(JsonObject args, string key) =>
            args[key] is JsonValue v && v.TryGetValue<decimal>(out var d) ? d : null;

        private static string? GetString(JsonObject args, string key) =>
            args[key] is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s) ? s : null;

        private static bool? GetBool(JsonObject args, string key) =>
            args[key] is JsonValue v && v.TryGetValue<bool>(out var b) ? b : null;
    }
}