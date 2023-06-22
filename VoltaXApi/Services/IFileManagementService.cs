
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
namespace VoltaXApi.Services
{
    public interface IFileManagementService
    {
        void UploadFile(string fileName, string filePath, IFormFile file);
        
    }
}