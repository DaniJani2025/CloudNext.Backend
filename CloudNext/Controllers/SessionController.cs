using CloudNext.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserSessionService _userSessionService;

    public UserController(IUserSessionService userSessionService)
    {
        _userSessionService = userSessionService;
    }

    [HttpGet("encryption-key/{userId}")]
    public IActionResult GetUserKey(Guid userId)
    {
        var key = _userSessionService.GetEncryptionKey(userId);
        if (key is null)
            return NotFound("No key stored for this user.");

        return Ok(new { Key = key });
    }
}
