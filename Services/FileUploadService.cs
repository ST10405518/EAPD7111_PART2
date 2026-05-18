using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace EAPD7111_PART2.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string[] _allowedPdfExtensions = { ".pdf" };

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
            return allowedExtensions.Contains(extension);
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            if (!ValidateFile(file, _allowedPdfExtensions))
            {
                throw new InvalidOperationException("Only PDF files are allowed.");
            }

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, folder);
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return Path.Combine(folder, uniqueFileName);
        }

        public byte[] DownloadFile(string filePath)
        {
            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, filePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found.", fullPath);
            }

            return File.ReadAllBytes(fullPath);
        }
    }
}
