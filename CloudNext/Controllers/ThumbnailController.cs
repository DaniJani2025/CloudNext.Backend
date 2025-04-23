using CloudNext.Interfaces;
using CloudNext.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudNext.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ThumbnailController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IUserSessionService _userSessionService;

        public ThumbnailController(IFileService fileService, IUserSessionService userSessionService)
        {
            _fileService = fileService;
            _userSessionService = userSessionService;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetFolderThumbnails([FromQuery] Guid userId, [FromQuery] Guid? folderId)
        {
            var encryptionKey = _userSessionService.GetEncryptionKey(userId);
            if (encryptionKey == null)
                return Unauthorized("Session invalid or expired");

            var thumbnails = await _fileService.GetThumbnailsForFolderAsync(folderId, userId);
            return Ok(thumbnails);
        }
    }

}
