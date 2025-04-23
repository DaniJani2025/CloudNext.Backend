using CloudNext.Interfaces;
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

        public ThumbnailController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpGet("{folderId}")]
        public async Task<IActionResult> GetFolderThumbnails([FromQuery] Guid? folderId, [FromQuery] Guid userId)
        {
            var thumbnails = await _fileService.GetThumbnailsForFolderAsync(folderId, userId);
            return Ok(thumbnails);
        }
    }

}
