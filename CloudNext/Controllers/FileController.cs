using CloudNext.DTOs.UserFiles;
using CloudNext.Interfaces;
using CloudNext.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloudNext.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IUserSessionService _userSessionService;

        public FileController(IFileService fileService, IUserSessionService userSessionService)
        {
            _fileService = fileService;
            _userSessionService = userSessionService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadEncryptedFiles(
            [FromQuery] string? parentFolderId,
            [FromQuery] Guid userId,
            [FromForm] List<IFormFile> files)
        {
            var encryptionKey = _userSessionService.GetEncryptionKey(userId);
            if (encryptionKey == null)
                return Unauthorized("Session invalid or expired");

            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            Guid? folderId = null;
            if (Guid.TryParse(parentFolderId, out var parsedId))
                folderId = parsedId;

            var results = new List<FileUploadResultDto>();

            foreach (var file in files)
            {
                try
                {
                    var savedFile = await _fileService.SaveEncryptedFileAsync(file, folderId, userId);
                    results.Add(new FileUploadResultDto
                    {
                        FileId = savedFile.Id,
                        OriginalName = savedFile.OriginalName,
                        Size = savedFile.Size,
                        ContentType = savedFile.ContentType
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new { Error = ex.Message, FileName = file.FileName });
                }
            }

            return Ok(new
            {
                Message = "File upload process completed.",
                Results = results
            });
        }

        [HttpPost("download")]
        public async Task<IActionResult> DownloadFiles([FromBody] FileDownloadRequestDto request)
        {
            var encryptionKey = _userSessionService.GetEncryptionKey(request.UserId);
            if (encryptionKey == null)
                return Unauthorized("Session invalid or expired");

            if (request.FileIds == null || request.FileIds.Count == 0)
                return BadRequest("No file IDs provided.");

            var (data, fileName, contentType) = await _fileService.GetDecryptedFilesAsync(request.FileIds, request.UserId);
            return File(data, contentType, fileName);
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
