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

        public FolderService(IUserFolderRepository userFolderRepository)
        {
            _userFolderRepository = userFolderRepository;
        }

        public async Task<FolderResponseDto> CreateFolderAsync(CreateFolderDto dto)
        {
            Guid? parentFolderId = dto.ParentFolderId;

            if (!parentFolderId.HasValue)
            {
                var rootFolder = await _userFolderRepository.GetRootFolderAsync(dto.UserId);
                if (rootFolder == null)
                    throw new InvalidOperationException("Root folder not found for user.");

                parentFolderId = rootFolder.Id;
            }

            var existingFolder = await _userFolderRepository.GetFolderAsync(dto.UserId, parentFolderId, dto.FolderName);
            if (existingFolder != null)
                throw new InvalidOperationException("A folder with the same name already exists in this location.");

            var parent = await _userFolderRepository.GetFolderByIdAsync(parentFolderId.Value);
            if (parent == null)
                throw new InvalidOperationException("Parent folder not found.");

            string virtualPath = Path.Combine(parent.VirtualPath, dto.FolderName).Replace("\\", "/");

            var newFolder = new UserFolder
            {
                Name = dto.FolderName,
                UserId = dto.UserId,
                ParentFolderId = parent.Id,
                VirtualPath = virtualPath
            };

            await _userFolderRepository.AddFolderAsync(newFolder);

            return new FolderResponseDto
            {
                FolderId = newFolder.Id,
                Name = newFolder.Name,
                VirtualPath = newFolder.VirtualPath,
                ParentFolderId = newFolder.ParentFolderId
            };
        }
    }
}
