using Microsoft.AspNetCore.Http;

namespace VoltaXApi.Services
{
    public sealed record StoredFile(string RelativeUrl, string Extension, string ContentType);

    public interface IFileManagementService
    {
        /// <summary>Validates an image (type by magic bytes, size) and stores it under wwwroot/<paramref name="folderName"/>
        /// with a random name. Throws ValidationException for a rejected file.</summary>
        Task<StoredFile> SaveImageAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default);

        /// <summary>Stores an HTML email template outside wwwroot (never served) with a random name.</summary>
        Task<StoredFile> SaveEmailTemplateAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default);

        void DeleteFileFromRoot(string relativePath);
    }
}
