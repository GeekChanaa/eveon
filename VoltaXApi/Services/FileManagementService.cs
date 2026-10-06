using Microsoft.AspNetCore.Http;
using VoltaXApi.Exceptions;
using VoltaXApi.Helpers;

namespace VoltaXApi.Services
{
    public class FileManagementService : IFileManagementService
    {
        private const long EmailTemplateMaxBytes = 1024 * 1024;

        private readonly ILogger<FileManagementService> _logger;
        private readonly long _maxImageBytes;
        private readonly string _webRoot;
        private readonly string _privateRoot;

        public FileManagementService(ILogger<FileManagementService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _maxImageBytes = configuration.GetValue<long?>("FileUploads:MaxBytes") ?? ImageUploadValidator.DefaultMaxBytes;
            _webRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            _privateRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "App_Data"));
        }

        public async Task<StoredFile> SaveImageAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default)
        {
            var detected = ImageUploadValidator.Validate(file, _maxImageBytes);
            var fileName = Guid.NewGuid().ToString("N") + detected.Extension;
            await WriteAsync(_webRoot, folderName, fileName, file, cancellationToken);
            return new StoredFile($"{folderName.Trim('/')}/{fileName}", detected.Extension, detected.ContentType);
        }

        public async Task<StoredFile> SaveEmailTemplateAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0) throw new ValidationException("Choose an email template to upload.");
            if (file.Length > EmailTemplateMaxBytes) throw new ValidationException("Email templates must be at most 1 MB.");
            var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
            if (extension is not ".html" and not ".htm") throw new ValidationException("Email templates must be .html files.");

            var fileName = Guid.NewGuid().ToString("N") + ".html";
            await WriteAsync(_privateRoot, folderName, fileName, file, cancellationToken);
            return new StoredFile($"{folderName.Trim('/')}/{fileName}", ".html", "text/html");
        }

        public void DeleteFileFromRoot(string relativePath)
        {
            var absolutePath = Contained(_webRoot, relativePath);
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
            else
                _logger.LogWarning("File to delete was not found: {Path}", relativePath);
        }

        private async Task WriteAsync(string root, string folderName, string fileName, IFormFile file, CancellationToken cancellationToken)
        {
            var folder = Contained(root, folderName);
            var fullPath = Contained(folder, fileName);
            try
            {
                Directory.CreateDirectory(folder);
                await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
                await file.CopyToAsync(stream, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Storing upload {FileName} in {Folder} failed", fileName, folderName);
                if (File.Exists(fullPath)) File.Delete(fullPath);
                throw;
            }
        }

        /// <summary>Resolves <paramref name="relative"/> under <paramref name="root"/> and refuses anything that escapes it.</summary>
        private static string Contained(string root, string relative)
        {
            var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(Path.Combine(rootFull, relative.TrimStart('/', '\\')));
            if (!(full + Path.DirectorySeparatorChar).StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Invalid file path.");
            return full;
        }
    }
}
