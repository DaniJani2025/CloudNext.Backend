using CloudNext.DTOs.UserFiles;
using CloudNext.Interfaces;
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

        public FileController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadEncryptedFiles([FromForm] Guid parentFolderId, [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var results = new List<FileUploadResultDto>();

            foreach (var file in files)
            {
                try
                {
                    var savedFile = await _fileService.SaveEncryptedFileAsync(file, parentFolderId);
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
            if (request.FileIds == null || request.FileIds.Count == 0)
                return BadRequest("No file IDs provided.");

            var (data, fileName, contentType) = await _fileService.GetDecryptedFilesAsync(request.FileIds, request.UserId);
            return File(data, contentType, fileName);
        }
    }
}
