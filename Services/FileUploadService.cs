using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AtharERP_System.Services
{
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
        public string? FileType { get; set; }
        public long FileSize { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class FileUploadService
    {
        private readonly IWebHostEnvironment _environment;

        // حسب ملاحظة الحقل file_type في القسم 3.12: image, pdf, dwg, docx
        private static readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp",
            ".pdf", ".dwg", ".doc", ".docx"
        };

        private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 ميجابايت

        public FileUploadService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<FileUploadResult> SaveFileAsync(IFormFile file, string subfolder)
        {
            if (file == null || file.Length == 0)
                return new FileUploadResult { Success = false, ErrorMessage = "لم يتم اختيار ملف" };

            if (file.Length > MaxFileSizeBytes)
                return new FileUploadResult { Success = false, ErrorMessage = "حجم الملف يتجاوز الحد المسموح (25 ميجابايت)" };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return new FileUploadResult { Success = false, ErrorMessage = "نوع الملف غير مسموح به (الأنواع المسموحة: صور، PDF، DWG، DOCX)" };

            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", subfolder);
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return new FileUploadResult
            {
                Success = true,
                FilePath = $"/uploads/{subfolder}/{safeFileName}",
                FileType = extension.TrimStart('.'),
                FileSize = file.Length
            };
        }

        public void DeleteFile(string relativePath)
        {
            var fullPath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}