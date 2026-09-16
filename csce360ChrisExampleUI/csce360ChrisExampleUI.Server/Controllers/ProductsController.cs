using System.Net.Http.Json;
using csce360ChrisExampleUI.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace csce360ChrisExampleUI.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IHttpClientFactory httpClientFactory, ILogger<ProductsController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet(Name = "GetProducts")]
        public async Task<ActionResult<IEnumerable<ProductResult>>> Get(
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? category,
            [FromQuery] bool? onSale,
            [FromQuery] string? companyName)
        {
            var client = _httpClientFactory.CreateClient("ProductApi");
            var requestUri = BuildProductQuery(minPrice, maxPrice, category, onSale, companyName);

            try
            {
                var results = await client.GetFromJsonAsync<IEnumerable<ProductResult>>(requestUri);
                return Ok(results ?? Enumerable.Empty<ProductResult>());
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to reach the product API");
                return StatusCode(StatusCodes.Status502BadGateway, "Could not reach the product API.");
            }
        }

        // GET /categories - proxies the API's GET /Category, for populating
        // the category filter dropdown.
        [HttpGet("~/categories", Name = "GetCategories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var client = _httpClientFactory.CreateClient("ProductApi");

            try
            {
                var results = await client.GetFromJsonAsync<IEnumerable<string>>("Category");
                return Ok(results ?? Enumerable.Empty<string>());
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to reach the product API");
                return StatusCode(StatusCodes.Status502BadGateway, "Could not reach the product API.");
            }
        }

        // GET /companies - proxies the API's GET /Company, for populating
        // the company filter dropdown.
        [HttpGet("~/companies", Name = "GetCompanies")]
        public async Task<ActionResult<IEnumerable<string>>> GetCompanies()
        {
            var client = _httpClientFactory.CreateClient("ProductApi");

            try
            {
                var results = await client.GetFromJsonAsync<IEnumerable<string>>("Company");
                return Ok(results ?? Enumerable.Empty<string>());
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to reach the product API");
                return StatusCode(StatusCodes.Status502BadGateway, "Could not reach the product API.");
            }
        }

        private static string BuildProductQuery(
            decimal? minPrice, decimal? maxPrice, string? category, bool? onSale, string? companyName)
        {
            var query = new Dictionary<string, string?>
            {
                ["minPrice"] = minPrice?.ToString(),
                ["maxPrice"] = maxPrice?.ToString(),
                ["category"] = category,
                ["onSale"] = onSale?.ToString(),
                ["companyName"] = companyName
            };

            var queryString = string.Join("&",
                query.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                     .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));

            return "Product" + (queryString.Length > 0 ? $"?{queryString}" : string.Empty);
        }
    }
}