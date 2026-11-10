namespace HelpDeskPro.UnitTests.Common;

using System.Text;
using FluentAssertions;
using HelpDeskPro.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

public class LocalFileStorageTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "HelpDeskPro_FileStorageTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:BasePath"] = _testDirectory
            })
            .Build();

        _storage = new LocalFileStorage(configuration);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in temp
            }
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SaveFileAsync_ValidStream_SavesFileWithGuidNameAndReturnsPath()
    {
        // Arrange
        const string content = "Ini isi file uji";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        const string originalName = "dokumen.pdf";

        // Act
        var storedName = await _storage.SaveFileAsync(stream, originalName);

        // Assert
        storedName.Should().EndWith(".pdf");
        var fullPath = Path.Combine(_testDirectory, storedName);
        File.Exists(fullPath).Should().BeTrue();
        var savedContent = await File.ReadAllTextAsync(fullPath);
        savedContent.Should().Be(content);
    }

    [Fact]
    public async Task GetFileAsync_ExistingFile_ReturnsReadableStream()
    {
        // Arrange
        const string content = "Konten untuk dibaca";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var storedName = await _storage.SaveFileAsync(stream, "laporan.txt");

        // Act
        using var resultStream = await _storage.GetFileAsync(storedName);

        // Assert
        resultStream.Should().NotBeNull();
        using var reader = new StreamReader(resultStream!);
        var readContent = await reader.ReadToEndAsync();
        readContent.Should().Be(content);
    }

    [Fact]
    public async Task GetFileAsync_NonExistingFile_ReturnsNull()
    {
        // Act
        var resultStream = await _storage.GetFileAsync("tidak_ada.pdf");

        // Assert
        resultStream.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_DeletesFileSuccessfully()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hapus"));
        var storedName = await _storage.SaveFileAsync(stream, "hapus.log");

        // Act
        await _storage.DeleteFileAsync(storedName);

        // Assert
        File.Exists(Path.Combine(_testDirectory, storedName)).Should().BeFalse();
    }
}
