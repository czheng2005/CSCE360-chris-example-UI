using ModelContextProtocol.Client;

namespace csce360ChrisExampleUI.Server.Mcp
{
    // Lazily creates and caches a single MCP client connection to this app's
    // own /mcp endpoint. A Lazy<Task<T>> wrapper is used because the built-in
    // DI container has no async factory support for AddSingleton, and the MCP
    // client is meant to be a long-lived connection rather than something
    // recreated per request.
    public class McpClientProvider
    {
        private readonly IConfiguration _configuration;
        private readonly Lazy<Task<McpClient>> _client;

        public McpClientProvider(IConfiguration configuration)
        {
            _configuration = configuration;
            _client = new Lazy<Task<McpClient>>(CreateClientAsync);
        }

        public Task<McpClient> GetClientAsync() => _client.Value;

        private async Task<McpClient> CreateClientAsync()
        {
            var mcpBaseUrl = _configuration["Mcp:BaseUrl"]
                ?? throw new InvalidOperationException("Configuration value 'Mcp:BaseUrl' was not found.");

            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(mcpBaseUrl)
            });

            // NOTE: McpClientFactory was removed in ModelContextProtocol 2.2.0 in favor
            // of a more DI-oriented construction path. This is the piece to verify
            // against whatever 2.2.0.x version actually restores — depending on the
            // exact release this may be:
            //   - McpClient.CreateAsync(transport)
            //   - new McpClient(transport) followed by an explicit ConnectAsync()
            //   - a services.AddMcpClient(...) DI extension where you resolve
            //     IMcpClient straight from the container instead of building it here
            // Check IntelliSense on the installed package / its release notes and
            // adjust this one call. Everything else in this file (the lazy-singleton
            // wrapper, GetClientAsync signature) stays correct regardless.
            return await McpClient.CreateAsync(transport);
        }
    }
}