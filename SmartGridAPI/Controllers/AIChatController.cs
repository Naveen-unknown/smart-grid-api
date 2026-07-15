using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGridAPI.DTOs;
using SmartGridAPI.Services;

namespace SmartGridAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AIChatController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AIChatController> _logger;

        public AIChatController(IAIService aiService, ILogger<AIChatController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        [HttpPost("message")]
        public async Task<IActionResult> GetChatResponse([FromBody] ChatRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request payload", errors = ModelState });

                var reply = await _aiService.GetChatResponseAsync(request.Message, request.History);
                
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        response = reply,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat response in AIChatController");
                return StatusCode(500, new { success = false, message = "Error getting AI response" });
            }
        }
    }
}
