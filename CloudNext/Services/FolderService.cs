using CloudNext.DTOs.UserFolder;
using CloudNext.Interfaces;
using CloudNext.Models;
using CloudNext.Repositories.Users;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace CloudNext.Services
{
    public class FolderService
    {
        private readonly IUserFolderRepository _userFolderRepository;
        private readonly IUserSessionService _userSessionService;

        public FolderService(IUserFolderRepository userFolderRepository, IUserSessionService userSessionService)
        {
            _userFolderRepository = userFolderRepository;
            _userSessionService = userSessionService;
        }

        public async Task<FolderResponseDto> CreateFolderAsync(CreateFolderDto dto)
        {
            var existingFolder = await _userFolderRepository.GetFolderAsync(dto.UserId, dto.ParentFolderId, dto.FolderName);
            if (existingFolder != null)
                throw new InvalidOperationException("A folder with the same name already exists in this location.");

            var basePath = Path.Combine(AppContext.BaseDirectory, "Documents", dto.UserId.ToString());
            string relativePath = dto.FolderName;

            if (dto.ParentFolderId.HasValue)
            {
                UserFolder? parent;

                if (dto.ParentFolderId.Value == dto.UserId)
                {
                    parent = new UserFolder
                    {
                        Id = dto.UserId,
                        Name = dto.UserId.ToString(),
                        ParentFolderId = null
                    };
                }
                else
                {
                    parent = await _userFolderRepository.GetFolderByIdAsync(dto.ParentFolderId.Value);
                    if (parent == null)
                        throw new InvalidOperationException("Parent folder not found.");
                }

                var nestedRelativePath = Path.Combine(GetFolderPath(parent), dto.FolderName).Replace("\\", "/");
                relativePath = nestedRelativePath;
                basePath = Path.Combine(basePath, GetFolderPath(parent));
            }

            var fullPath = Path.Combine(basePath, dto.FolderName);
            Directory.CreateDirectory(fullPath);

            var newFolder = new UserFolder
            {
                Name = dto.FolderName,
                UserId = dto.UserId,
                ParentFolderId = dto.ParentFolderId,
                RelativePath = relativePath
            };

            await _userFolderRepository.AddFolderAsync(newFolder);
            return new FolderResponseDto
            {
                UserId = newFolder.UserId,
                Name = newFolder.Name,
                RelativePath = newFolder.RelativePath,
                ParentFolderId = newFolder.ParentFolderId
            };
        }

        private string GetFolderPath(UserFolder folder)
        {
            if (folder.ParentFolderId == null || folder.Id == folder.ParentFolderId)
                return folder.Name;

            return Path.Combine(folder.ParentFolder?.RelativePath ?? folder.Name);
        }
    }
}
