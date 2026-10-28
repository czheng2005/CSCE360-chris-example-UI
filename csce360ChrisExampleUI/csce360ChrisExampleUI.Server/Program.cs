using csce360ChrisExampleUI.Server.Mcp;
using csce360ChrisExampleUI.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient("ProductApi", client =>
{
    var baseUrl = builder.Configuration["ProductApi:BaseUrl"]
        ?? throw new InvalidOperationException("Configuration value 'ProductApi:BaseUrl' was not found.");
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient("Gemini", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
});

// ProductTools is injected directly into NaturalLanguageSearchService.
builder.Services.AddTransient<ProductTools>();

// The MCP endpoint is still exposed for external MCP clients (Inspector,
// Claude Desktop, etc.), but the app itself no longer connects to it.
// Stateless: the tools hold no per-session state.
builder.Services
    .AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithTools<ProductTools>();

builder.Services.AddScoped<NaturalLanguageSearchService>();

var app = builder.Build();

app.MapMcp("/mcp");
app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();