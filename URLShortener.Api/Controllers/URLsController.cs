using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using URLShortener.Application.Dtos;
using URLShortener.Application.Services;

namespace URLShortener.Api.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    public class UrlsController : ControllerBase
    {
        private readonly IUrlService _urlService;
        private readonly IValidator<CreateUrlRequest> _createValidator;
        private readonly IValidator<UpdateUrlRequest> _updateValidator;
        private readonly ILogger<UrlsController> _logger;

        public UrlsController(
            IUrlService urlService,
            IValidator<CreateUrlRequest> createValidator,
            IValidator<UpdateUrlRequest> updateValidator,
            ILogger<UrlsController> logger)
        {
            _urlService = urlService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<UrlResponse>> CreateUrl([FromBody] CreateUrlRequest request)
        {
            if (!request.IsValid)
            {
                return BadRequest("Invalid request: Original URL is required");
            }
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }
            if (request.HasExpiration && !request.IsExpirationValid)
            {
                return BadRequest("Expiration date must be in the future");
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _urlService.CreateUrlAsync(request, baseUrl);

            if (result == null)
            {
                return Conflict("Custom alias already exists or creation failed");
            }

            return CreatedAtAction(nameof(GetUrlDetails), new { shortCode = result.ShortCode }, result);
        }
        [HttpGet("{shortCode}")]
        public async Task<ActionResult<UrlDetailsResponse>> GetUrlDetails(string shortCode)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _urlService.GetUrlDetailsAsync(shortCode, baseUrl);

            if (result == null)
            {
                return NotFound("Short URL not found");
            }

            return Ok(result);
        }
        [HttpPut("{shortCode}")]
        public async Task<IActionResult> UpdateUrl(string shortCode, [FromBody] UpdateUrlRequest request)
        {
            if (!request.HasAnyUpdates)
            {
                return BadRequest("No updates provided");
            }
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }
            if (request.HasExpirationUpdate && !request.IsExpirationValid)
            {
                return BadRequest("Expiration date must be in the future");
            }

            var success = await _urlService.UpdateUrlAsync(shortCode, request);
            if (!success)
            {
                return NotFound("Short URL not found");
            }

            return NoContent();
        }
        [HttpDelete("{shortCode}")]
        public async Task<IActionResult> DeleteUrl(string shortCode)
        {
            var success = await _urlService.DeleteUrlAsync(shortCode);
            if (!success)
            {
                return NotFound("Short URL not found");
            }

            return NoContent();
        }
    }
