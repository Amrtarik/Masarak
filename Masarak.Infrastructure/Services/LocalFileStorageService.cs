using Masarak.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Masarak.Infrastructure.Services
{
    /// <summary>
    /// Local file system implementation of IFileStorageService for development.
    /// In production, this should be replaced with AzureBlobStorageService.
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocalFileStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(string BlobName, string PublicUrl)> UploadAsync(Stream content, string fileName, string containerName, CancellationToken ct = default)
        {
            // Create a unique filename
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
            
            // Determine the upload directory (e.g., wwwroot/uploads/{containerName})
            var uploadPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", containerName);
            
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var filePath = Path.Combine(uploadPath, uniqueFileName);

            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await content.CopyToAsync(fileStream, ct);
            }

            // Generate the local public URL (assuming static files are served)
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "http://localhost:5000"; // fallback
            
            var publicUrl = $"{baseUrl}/uploads/{containerName}/{uniqueFileName}";

            return (uniqueFileName, publicUrl);
        }

        public Task<string> GenerateSignedDownloadUrlAsync(string blobName, string containerName, TimeSpan expiry, CancellationToken ct = default)
        {
            // For local dev, just return the public URL since we aren't enforcing SAS tokens locally
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "http://localhost:5000";
            
            return Task.FromResult($"{baseUrl}/uploads/{containerName}/{blobName}");
        }

        public Task DeleteAsync(string blobName, string containerName, CancellationToken ct = default)
        {
            var filePath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", containerName, blobName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            return Task.CompletedTask;
        }
    }
}
