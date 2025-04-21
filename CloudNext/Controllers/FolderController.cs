using CloudNext.DTOs.UserFolder;
using CloudNext.Interfaces;
using CloudNext.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloudNext.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FolderController : ControllerBase
    {
        private readonly FolderService _folderService;

        public FolderController(FolderService folderService)
        {
            _folderService = folderService;
        }

        [HttpPost("create-folder")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto dto)
        {
            try
            {
                var result = await _folderService.CreateFolderAsync(dto);
                return Ok(new
                {
                    Message = "Folder created successfully.",
                    Results = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("download")]
        public async Task<IActionResult> DownloadFolder([FromBody] FolderDownloadRequestDto dto)
        {
            try
            {
                var zipFileBytes = await _folderService.DownloadFolderAsync(dto.UserId, dto.FolderId);

                return File(zipFileBytes, "application/zip", "folder.zip");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFolder([FromForm] FolderUploadDto dto)
        {
            try
            {
                await _folderService.UploadFolderAsync(dto.UserId, dto);
                return Ok("Folder uploaded successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
