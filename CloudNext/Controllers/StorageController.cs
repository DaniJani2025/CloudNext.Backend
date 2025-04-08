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

        [HttpPost("upload/{userId}")]
        public async Task<IActionResult> UploadEncryptedFiles(Guid userId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var results = new List<object>();

            foreach (var file in files)
            {
                try
                {
                    var savedFile = await _fileService.SaveEncryptedFileAsync(file, userId);
                    results.Add(new
                    {
                        FilePath = savedFile.FilePath,
                        Size = savedFile.Size,
                        ContentType = savedFile.ContentType,
                        Name = savedFile.Name
                    });
                }
                catch (InvalidOperationException ex)
                {
                    results.Add(new { Error = ex.Message, FileName = file.FileName });
                }
            }

            return Ok(new
            {
                Message = "File upload process completed.",
                UploadedFiles = results
            });
        }
    }
}
