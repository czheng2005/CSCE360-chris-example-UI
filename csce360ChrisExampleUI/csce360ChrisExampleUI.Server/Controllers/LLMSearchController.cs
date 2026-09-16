using csce360ChrisExampleUI.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace csce360ChrisExampleUI.Server.Controllers
{
    [ApiController]
    [Route("Products")]
    public class NaturalLanguageSearchController : ControllerBase
    {
        private readonly NaturalLanguageSearchService _service;

        public NaturalLanguageSearchController(NaturalLanguageSearchService service)
        {
            _service = service;
        }

        public record NlSearchRequest(string Query);

        [HttpPost("nl-search")]
        public async Task<IActionResult> NlSearch([FromBody] NlSearchRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest("Query is required.");
            }

            var result = await _service.RunAsync(request.Query, ct);
            return Ok(result);
        }
    }
}