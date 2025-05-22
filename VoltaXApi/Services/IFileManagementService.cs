
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
namespace VoltaXApi.Services
{
    public interface IFileManagementService
    {
        void UploadFile(string fileName, string filePath, IFormFile file);
        void UploadImage(string fileName, string filePath, IFormFile file);
        void UploadEmailTemplate(string fileName, string filePath, IFormFile file);
        void DeleteFileFromRoot(string relativePath);
        
    }
}