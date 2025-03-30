//using System;
//using System.IO;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Webapp.Database;


//namespace User_management_system.Utils
//{
//    public class FileService
//    {
//        private static readonly string _documentsRoot = "Documents";
//        private readonly DatabaseManager _database = new();

//        public async Task<(bool Success, string Message)> UploadFileAsync(IFormFile file, int userId, string userFolderPath)
//        {
//            if (file == null || file.Length == 0)
//            {
//                return (false, "No file provided.");
//            }

//            if (!Directory.Exists(userFolderPath))
//            {
//                Directory.CreateDirectory(userFolderPath);
//            }

//            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
//            var fileName = Path.GetFileName(file.FileName);
//            string baseFileName = Path.GetFileNameWithoutExtension(fileName);
//            string fileExtension = Path.GetExtension(fileName).TrimStart('.');

//            // Ensure uniqueness within the same request
//            int copyIndex = 1;
//            string newFileName = $"{baseFileName}_{timestamp}.{fileExtension}";
//            var filePath = Path.Combine(userFolderPath, newFileName);

//            while (File.Exists(filePath))  // Prevents overwriting in local storage
//            {
//                newFileName = $"{baseFileName}_{timestamp}_copy{copyIndex}.{fileExtension}";
//                filePath = Path.Combine(userFolderPath, newFileName);
//                copyIndex++;
//            }

//            bool filePathSaved = _database.StoreFilePath(userId, newFileName, fileExtension, timestamp);

//            if (!filePathSaved)
//            {
//                Console.WriteLine("File Path not Saved");
//            }

//            try
//            {
//                using (var fileStream = new FileStream(filePath, FileMode.Create))
//                {
//                    await file.CopyToAsync(fileStream);
//                }

//                return (true, "File uploaded successfully.");
//            }
//            catch (Exception ex)
//            {
//                return (false, $"An error occurred while uploading the file: {ex.Message}");
//            }
//        }

//        public List<string> GetUserFiles(int userId)
//        {
//            string userFolderPath = _database.FetchUserFolderPath(userId);
//            string fullPath = Path.Combine(_documentsRoot, userFolderPath);

//            var files = Directory.GetFiles(fullPath);
//            var fileNames = new List<string>();

//            foreach (var file in files)
//            {
//                fileNames.Add(Path.GetFileName(file));
//            }

//            return fileNames;
//        }

//        public string CreateUserFolder(string folderName)
//        {
//            // Ensure the Documents directory exists
//            if (!Directory.Exists(_documentsRoot))
//            {
//                Directory.CreateDirectory(_documentsRoot);
//            }

//            // Create the full path for the user's folder
//            var userFolderPath = Path.Combine(_documentsRoot, folderName);

//            // Check if the folder already exists
//            if (!Directory.Exists(userFolderPath))
//            {
//                Directory.CreateDirectory(userFolderPath);
//            }

//            return userFolderPath; // Return the created folder path
//        }
//    }
//}
