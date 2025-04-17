using CloudNext.Interfaces;
using CloudNext.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CloudNext.Controllers
{
    [ApiController]
    [Route("api/session")]
    public class SessionController : ControllerBase
    {
        private readonly IUserSessionService _sessionService;

        public SessionController(IUserSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpGet("key")]
        public IActionResult GetKey([FromQuery] Guid userId)
        {
            var key = _sessionService.GetEncryptionKey(userId);
            if (key == null)
                return NotFound("Key not found");

            return Ok(new { Key = key });
        }
    }
}