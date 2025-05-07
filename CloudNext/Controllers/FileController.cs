using CloudNext.DTOs.UserFiles;
using CloudNext.Interfaces;
using CloudNext.Common;
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
            var skippedCount = 0;

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (!Constants.Media.SupportedImageTypes.Contains(file.ContentType)
                && !Constants.Media.SupportedVideoTypes.Contains(file.ContentType)
                && !Constants.Media.CommonFileLogos.ContainsKey(ext))
                {
                    skippedCount++;
                    continue;
                }

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
                    skippedCount++;
                    return BadRequest(new { Error = ex.Message, FileName = file.FileName });
                }
            }
            var message = skippedCount > 0
                    ? "Some files were not uploaded."
                    : "File upload process completed.";
            
            return Ok(new
            {
                Message = message,
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

        [HttpGet("stream/{fileId}")]
        public async Task<IActionResult> StreamVideo(Guid fileId, [FromQuery] string userId)
        {
            var rangeHeader = Request.Headers["Range"].ToString();

            var result = await _fileService.StreamDecryptedVideoAsync(fileId, userId, rangeHeader);

            Response.StatusCode = StatusCodes.Status206PartialContent;
            Response.ContentLength = result.ContentLength;
            Response.Headers["Content-Range"] = result.ContentRange;
            Response.Headers["Accept-Ranges"] = "bytes";

            return new FileStreamResult(result.Stream, result.ContentType);
        }
    }
}
