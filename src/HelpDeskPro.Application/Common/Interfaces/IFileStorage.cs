namespace HelpDeskPro.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<string> SaveFileAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task<Stream?> GetFileAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default);
}

