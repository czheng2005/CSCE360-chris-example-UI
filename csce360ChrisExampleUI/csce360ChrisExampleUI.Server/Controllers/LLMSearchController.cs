using csce360ChrisExampleUI.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace csce360ChrisExampleUI.Server.Controllers
{
    [ApiController]
    [Route("Products")]
    public class NaturalLanguageSearchController : ControllerBase
    {
        private readonly NaturalLanguageSearchService _service;
        private readonly ILogger<NaturalLanguageSearchController> _logger;

        public NaturalLanguageSearchController(
            NaturalLanguageSearchService service,
            ILogger<NaturalLanguageSearchController> logger)
        {
            _service = service;
            _logger = logger;
        }

        public record NlSearchRequest(string Query);

        [HttpPost("nl-search")]
        public async Task<IActionResult> NlSearch([FromBody] NlSearchRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest("Query is required.");
            }

            try
            {
                var result = await _service.RunAsync(request.Query, ct);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                // Client went away; nothing to return.
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Natural language search failed for query: {Query}", request.Query);
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status502BadGateway);
            }
        }
    }
}