using CloudNext.DTOs.UserFolder;
using CloudNext.Interfaces;
using CloudNext.Models;
using CloudNext.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CloudNext.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FolderController : ControllerBase
    {
        private readonly IFolderService _folderService;
        private readonly IUserSessionService _userSessionService;

        public FolderController(IFolderService folderService, IUserSessionService userSessionService)
        {
            _folderService = folderService;
            _userSessionService = userSessionService;
        }

        [HttpPost("create-folder")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto dto)
        {
            var encryptionKey = await _userSessionService.GetEncryptionKey(dto.UserId);
            if (encryptionKey == null)
                return Unauthorized("Session invalid or expired");

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
            var encryptionKey = await _userSessionService.GetEncryptionKey(dto.UserId);
            if (encryptionKey == null)
                return Unauthorized("Session invalid or expired");

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
            var encryptionKey = await _userSessionService.GetEncryptionKey(dto.UserId);
            if (encryptionKey == null)
                return Unauthorized("Session invalid or expired");

            try
            {
                var result = await _folderService.UploadFolderAsync(dto.UserId, dto);
                var msg = result.SkippedCount > 0
                            ? "Folder uploaded—with some files skipped."
                            : "Folder uploaded successfully.";
                return Ok(new
                {
                    Message = msg,
                    UploadedCount = result.UploadedCount,
                    SkippedCount = result.SkippedCount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllFolders([FromQuery] Guid userId, [FromQuery] Guid? folderId)
        {
            var encryptionKey = await _userSessionService.GetEncryptionKey(userId);
            if (encryptionKey == null)
                return Unauthorized("Session invalid or expired");

            try
            {
                var folders = await _folderService.GetFoldersInCurrentDirectoryAsync(userId, folderId);
                return Ok(folders);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("structure")]
        public async Task<IActionResult> GetFullFolderStructure([FromQuery] Guid userId)
        {
            var encryptionKey = await _userSessionService.GetEncryptionKey(userId);
            if (encryptionKey == null)
                return Unauthorized("Session invalid or expired");

            try
            {
                var structure = await _folderService.GetFullFolderStructureAsync(userId);
                return Ok(structure);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
