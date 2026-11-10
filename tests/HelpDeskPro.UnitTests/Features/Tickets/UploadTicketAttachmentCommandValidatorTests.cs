namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Features.Tickets.Commands.UploadTicketAttachment;
using Xunit;

public class UploadTicketAttachmentCommandValidatorTests
{
    private readonly UploadTicketAttachmentCommandValidator _validator = new();

    [Theory]
    [InlineData("foto.jpg")]
    [InlineData("foto.jpeg")]
    [InlineData("gambar.png")]
    [InlineData("dokumen.pdf")]
    [InlineData("laporan.docx")]
    [InlineData("data.xlsx")]
    [InlineData("catatan.txt")]
    [InlineData("system.log")]
    public void Validate_AllowedFileExtensions_ShouldSucceed(string fileName)
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        var command = new UploadTicketAttachmentCommand(1, stream, fileName, "application/octet-stream", 1024);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("program.exe")]
    [InlineData("script.bat")]
    [InlineData("run.cmd")]
    [InlineData("archive.zip")]
    [InlineData("malware.dll")]
    [InlineData("tanpa_ekstensi")]
    public void Validate_DisallowedFileExtensions_ThrowsValidationErrorWithAC06Message(string fileName)
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        var command = new UploadTicketAttachmentCommand(1, stream, fileName, "application/octet-stream", 1024);

        // Act
        var result = _validator.Validate(command);

        // Assert (AC-06)
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName" && e.ErrorMessage == "Tipe file tidak diizinkan.");
    }

    [Fact]
    public void Validate_FileExceeds5MB_ThrowsValidationError()
    {
        // Arrange
        const long size6Mb = 6 * 1024 * 1024;
        using var stream = new MemoryStream();
        var command = new UploadTicketAttachmentCommand(1, stream, "dokumen.pdf", "application/pdf", size6Mb);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileSizeBytes" && e.ErrorMessage == "Ukuran file melebihi batas maksimal 5 MB.");
    }

    [Fact]
    public void Validate_EmptyFile_ThrowsValidationError()
    {
        // Arrange
        using var stream = new MemoryStream();
        var command = new UploadTicketAttachmentCommand(1, stream, "dokumen.pdf", "application/pdf", 0);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileSizeBytes" && e.ErrorMessage == "File tidak boleh kosong.");
    }

    [Fact]
    public void Validate_InvalidTicketId_ThrowsValidationError()
    {
        // Arrange
        using var stream = new MemoryStream([1]);
        var command = new UploadTicketAttachmentCommand(0, stream, "dokumen.pdf", "application/pdf", 1);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TicketId" && e.ErrorMessage == "ID tiket tidak valid.");
    }
}

