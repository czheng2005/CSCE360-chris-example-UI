using System.ComponentModel;
using System.Net.Http.Json;
using csce360ChrisExampleUI.Server.Models;
using ModelContextProtocol.Server;

namespace csce360ChrisExampleUI.Server.Mcp
{
    [McpServerToolType]
    public class ProductTools
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductTools(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [McpServerTool(Name = "search_products")]
        [Description("Search products, optionally filtered by price range, category, on-sale status, and/or company name.")]
        public async Task<IEnumerable<ProductResult>> SearchProducts(
            [Description("Minimum price, inclusive. Omit for no lower bound.")] decimal? minPrice = null,
            [Description("Maximum price, inclusive. Omit for no upper bound.")] decimal? maxPrice = null,
            [Description("Exact category name, e.g. 'Hardware'. Omit for all categories.")] string? category = null,
            [Description("True = only on-sale items, false = only non-sale items, omit for both.")] bool? onSale = null,
            [Description("Exact company/vendor name. Omit for all companies.")] string? companyName = null)
        {
            var client = _httpClientFactory.CreateClient("ProductApi");

            var filter = new
            {
                priceRange = new { minPrice = minPrice ?? 0, maxPrice = maxPrice ?? decimal.MaxValue },
                category,
                onSale,
                companyName
            };

            var response = await client.PostAsJsonAsync("Product/search", filter);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<IEnumerable<ProductResult>>()
                ?? Enumerable.Empty<ProductResult>();
        }

        [McpServerTool(Name = "list_categories")]
        [Description("List all distinct product category names. Use this to check valid values for search_products' category parameter before guessing.")]
        public async Task<IEnumerable<string>> ListCategories()
        {
            var client = _httpClientFactory.CreateClient("ProductApi");
            return await client.GetFromJsonAsync<IEnumerable<string>>("Category")
                ?? Enumerable.Empty<string>();
        }

        [McpServerTool(Name = "list_companies")]
        [Description("List all distinct company/vendor names. Use this to check valid values for search_products' companyName parameter before guessing.")]
        public async Task<IEnumerable<string>> ListCompanies()
        {
            var client = _httpClientFactory.CreateClient("ProductApi");
            return await client.GetFromJsonAsync<IEnumerable<string>>("Company")
                ?? Enumerable.Empty<string>();
        }
    }
}