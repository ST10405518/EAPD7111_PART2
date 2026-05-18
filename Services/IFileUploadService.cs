namespace EAPD7111_PART2.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadFileAsync(IFormFile file, string folder);
        bool ValidateFile(IFormFile file, string[] allowedExtensions);
        byte[] DownloadFile(string filePath);
    }
}
