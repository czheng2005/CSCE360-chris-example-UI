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

        // Filter options (region, category, price range, etc.) can be added here later
        // as query parameters and forwarded to the upstream API call below.
        [HttpGet(Name = "GetProducts")]
        public async Task<ActionResult<IEnumerable<ProductResult>>> Get()
        {
            var client = _httpClientFactory.CreateClient("ProductApi");

            try
            {
                var results = await client.GetFromJsonAsync<IEnumerable<ProductResult>>("Product");
                return Ok(results ?? Enumerable.Empty<ProductResult>());
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to reach the product API");
                return StatusCode(StatusCodes.Status502BadGateway, "Could not reach the product API.");
            }
        }
    }
}
