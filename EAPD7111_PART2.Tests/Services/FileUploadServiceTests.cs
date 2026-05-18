using EAPD7111_PART2.Services;
using EAPD7111_PART2.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace EAPD7111_PART2.Tests.Services
{
    public class FileUploadServiceTests : IDisposable
    {
        private readonly string _tempWebRoot;
        private readonly FileUploadService _service;
        private static readonly string[] AllowedPdfExtensions = [".pdf"];

        public FileUploadServiceTests()
        {
            _tempWebRoot = Path.Combine(Path.GetTempPath(), "glms-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempWebRoot);

            var environment = new Mock<IWebHostEnvironment>();
            environment.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

            _service = new FileUploadService(environment.Object);
        }

        [Fact]
        public void ValidateFile_WithPdfExtension_ReturnsTrue()
        {
            var file = FormFileHelper.CreateMockFile("agreement.pdf", [0x25, 0x50, 0x44, 0x46], "application/pdf");

            var isValid = _service.ValidateFile(file, AllowedPdfExtensions);

            Assert.True(isValid);
        }

        [Fact]
        public void ValidateFile_WithExeExtension_ReturnsFalse()
        {
            var file = FormFileHelper.CreateMockFile("malware.exe", [0x4D, 0x5A], "application/octet-stream");

            var isValid = _service.ValidateFile(file, AllowedPdfExtensions);

            Assert.False(isValid);
        }

        [Fact]
        public async Task UploadFileAsync_WithExeFile_ThrowsInvalidOperationException()
        {
            var file = FormFileHelper.CreateMockFile("malware.exe", [0x4D, 0x5A]);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UploadFileAsync(file, "uploads/contracts"));

            Assert.Equal("Only PDF files are allowed.", exception.Message);
        }

        [Fact]
        public async Task UploadFileAsync_WithPdfFile_SavesFileAndReturnsRelativePath()
        {
            var file = FormFileHelper.CreateMockFile("signed.pdf", [0x25, 0x50, 0x44, 0x46], "application/pdf");

            var relativePath = await _service.UploadFileAsync(file, "uploads/contracts");

            Assert.EndsWith(".pdf", relativePath, StringComparison.OrdinalIgnoreCase);
            var fullPath = Path.Combine(_tempWebRoot, relativePath);
            Assert.True(File.Exists(fullPath));
        }

        [Fact]
        public void DownloadFile_WhenFileMissing_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => _service.DownloadFile("uploads/contracts/missing.pdf"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempWebRoot))
            {
                Directory.Delete(_tempWebRoot, recursive: true);
            }
        }
    }
}
