
using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace VoltaXApi.Services
{
    public class FileManagementService : IFileManagementService
    {

        public void UploadImage(string fileName, string folderName, IFormFile file)
        {
            if (!IsValidImageFile(file))
                throw new Exception("Invalid image format");

            UploadFile(fileName, folderName, file);
        }

        public void UploadFile(string fileName, string folderName, IFormFile file)
        {
            try
            {
                string newPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folderName);
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }
                if (file.Length > 0)
                {
                    string fullPath = Path.Combine(newPath, fileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void UploadEmailTemplate(string fileName, string filePath, IFormFile file)
        {
            try
            {
                string newPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }
                if (file.Length > 0)
                {
                    string fullPath = Path.Combine(newPath, fileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        private bool IsValidImageFile(IFormFile file)
        {
            if (file == null) return false;

            string[] permittedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            return permittedExtensions.Contains(fileExtension);
        }

        public void DeleteFileFromRoot(string relativePath)
        {
          var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
          var absolutePath = Path.Combine(webRootPath, relativePath);
          if (File.Exists(absolutePath))
          {
              File.Delete(absolutePath);
          }
          else
          {
              Console.WriteLine("File not found.");
          }
        }
    }
}