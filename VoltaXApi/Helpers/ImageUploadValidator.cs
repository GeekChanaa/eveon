using VoltaXApi.Exceptions;

namespace VoltaXApi.Helpers;

/// <summary>
/// Decides what an uploaded image really is from its first bytes. The client's file name and
/// Content-Type are ignored: only JPEG, PNG and WebP are accepted (no SVG/HTML, which a browser
/// would execute when the file is served from wwwroot).
/// </summary>
public static class ImageUploadValidator
{
    public const long DefaultMaxBytes = 5 * 1024 * 1024;

    public sealed record DetectedImage(string Extension, string ContentType);

    public static DetectedImage Validate(IFormFile? file, long maxBytes = DefaultMaxBytes)
    {
        if (file == null || file.Length == 0) throw new ValidationException("Choose an image to upload.");
        if (file.Length > maxBytes)
            throw new ValidationException($"Images must be at most {maxBytes / (1024 * 1024)} MB.");

        var header = new byte[12];
        int read;
        using (var stream = file.OpenReadStream())
        {
            read = 0;
            while (read < header.Length)
            {
                var n = stream.Read(header, read, header.Length - read);
                if (n == 0) break;
                read += n;
            }
        }

        return Detect(header.AsSpan(0, read))
               ?? throw new ValidationException("Upload a JPG, PNG or WebP image.");
    }

    public static DetectedImage? Detect(ReadOnlySpan<byte> h)
    {
        if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF)
            return new DetectedImage(".jpg", "image/jpeg");
        if (h.Length >= 8 && h[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return new DetectedImage(".png", "image/png");
        if (h.Length >= 12 && h[..4].SequenceEqual("RIFF"u8) && h[8..12].SequenceEqual("WEBP"u8))
            return new DetectedImage(".webp", "image/webp");
        return null;
    }
}
