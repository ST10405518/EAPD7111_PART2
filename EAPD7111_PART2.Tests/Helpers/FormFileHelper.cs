using Microsoft.AspNetCore.Http;
using Moq;

namespace EAPD7111_PART2.Tests.Helpers
{
    internal static class FormFileHelper
    {
        public static IFormFile CreateMockFile(string fileName, byte[] content, string contentType = "application/octet-stream")
        {
            var stream = new MemoryStream(content);
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream target, CancellationToken _) => stream.CopyToAsync(target));
            return mockFile.Object;
        }
    }
}
