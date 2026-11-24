using VCare.SharedKernel.Results;

namespace VCare.Api.Services
{
    /// <summary>
    /// Which files may be stored, and what they are served as. The type comes
    /// from the extension, from a short list, never from the content type the
    /// client claims: a file uploaded as "image/png" but holding HTML or SVG would
    /// otherwise run as a page in whoever opens it, with their session.
    /// </summary>
    public static class UploadPolicy
    {
        /// <summary>Letters, forms and scans: what a care record holds.</summary>
        public static readonly IReadOnlyDictionary<string, string> Documents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".heic"] = "image/heic",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".odt"] = "application/vnd.oasis.opendocument.text",
            [".rtf"] = "application/rtf",
            [".txt"] = "text/plain",
        };

        public static readonly IReadOnlyDictionary<string, string> ProfileImages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".heic"] = "image/heic",
        };

        public static Result<string> ContentTypeFor(string fileName, IReadOnlyDictionary<string, string> allowed)
        {
            var extension = Path.GetExtension(fileName ?? string.Empty);
            if (!string.IsNullOrEmpty(extension) && allowed.TryGetValue(extension, out var contentType))
                return Result.Success(contentType);
            return Result.Failure<string>(Error.Validation("Documents.FileTypeNotAllowed",
                $"Only these file types can be uploaded here: {string.Join(", ", allowed.Keys.Order())}."));
        }

        /// <summary>The name the file was uploaded with: keys are "{home}/{folder}/{id}_{name}".</summary>
        public static string DownloadName(string storageKey) => VCare.SharedKernel.Domain.StorageKeys.DownloadName(storageKey);
    }
}
