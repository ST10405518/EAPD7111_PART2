using Microsoft.AspNetCore.Hosting;

namespace EAPD7111_PART2.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string[] _allowedPdfExtensions = [".pdf"];

        public FileUploadService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public bool ValidateFile(IFormFile file, string[] allowedExtensions)
        {
            if (file == null || file.Length == 0)
            {
                return false;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".pdf";
            }

            return allowedExtensions.Contains(extension);
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            if (!ValidateFile(file, _allowedPdfExtensions))
            {
                throw new InvalidOperationException("Only PDF files are allowed.");
            }

            var uploadsFolder = Path.Combine(GetWebRootPath(), folder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid():N}.pdf";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            return Path.Combine(folder, uniqueFileName).Replace('\\', '/');
        }

        public byte[] DownloadFile(string filePath)
        {
            var normalizedPath = filePath.TrimStart('/', '\\').Replace('\\', '/');
            var fullPath = Path.Combine(GetWebRootPath(), normalizedPath);

            if (!System.IO.File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found.", fullPath);
            }

            return System.IO.File.ReadAllBytes(fullPath);
        }

        private string GetWebRootPath()
        {
            if (!string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath))
            {
                return _webHostEnvironment.WebRootPath;
            }

            var webRoot = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(webRoot);
            return webRoot;
        }
    }
}
