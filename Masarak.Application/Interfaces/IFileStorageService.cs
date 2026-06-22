namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Abstraction over file storage (Azure Blob in prod, local filesystem in dev).
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Uploads a file and returns the blob name and public URL.
        /// </summary>
        Task<(string BlobName, string PublicUrl)> UploadAsync(Stream content, string fileName, string containerName, CancellationToken ct = default);

        /// <summary>
        /// Generates a time-limited signed download URL for a blob.
        /// </summary>
        Task<string> GenerateSignedDownloadUrlAsync(string blobName, string containerName, TimeSpan expiry, CancellationToken ct = default);

        /// <summary>
        /// Deletes a blob from storage.
        /// </summary>
        Task DeleteAsync(string blobName, string containerName, CancellationToken ct = default);
    }
}
