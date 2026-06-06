namespace GLMS.Api.Services;

public interface IFileUploadService
{
    Task<string> UploadFileAsync(IFormFile file, string folder);
    bool ValidateFile(IFormFile file, string[] allowedExtensions);
    byte[] DownloadFile(string filePath);
}
