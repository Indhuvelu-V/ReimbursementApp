using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using ReimbursementTrackerApp.Services;
using System.Text;
using Xunit;

namespace ReimbursementTrackerApp.Tests.Services
{
    public class FileUploadServiceTests : IDisposable
    {
        private readonly Mock<IWebHostEnvironment> _env    = new();
        private readonly Mock<ILogger<FileUploadService>> _logger = new();
        private readonly string _tempDir;

        public FileUploadServiceTests()
        {
            // Create a real temp directory so actual file writes work
            _tempDir = Path.Combine(Path.GetTempPath(), $"FileUploadTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
            _env.Setup(e => e.WebRootPath).Returns(_tempDir);
        }

        public void Dispose()
        {
            // Clean up temp directory after each test class run
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        private FileUploadService CreateService() =>
            new(_env.Object, _logger.Object);

        // Helper — creates a mock IFormFile
        private IFormFile MakeFile(
            string fileName    = "test.pdf",
            string contentType = "application/pdf",
            long   length      = 1024,
            string content     = "fake file content")
        {
            var bytes  = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            // Use the explicit length — allows testing length=0 case
            fileMock.Setup(f => f.Length).Returns(length);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((s, _) => stream.CopyTo(s))
                .Returns(Task.CompletedTask);
            return fileMock.Object;
        }

        // ── SaveFileAsync — happy path ────────────────────────────────────────

        [Fact]
        public async Task SaveFileAsync_ValidPdf_ReturnsUploadUrl()
        {
            var file   = MakeFile("receipt.pdf", "application/pdf");
            var result = await CreateService().SaveFileAsync(file);

            result.Should().StartWith("/uploads/");
            result.Should().EndWith(".pdf");
        }

        [Fact]
        public async Task SaveFileAsync_ValidPng_ReturnsUploadUrl()
        {
            var file   = MakeFile("photo.png", "image/png");
            var result = await CreateService().SaveFileAsync(file);

            result.Should().StartWith("/uploads/");
            result.Should().EndWith(".png");
        }

        [Fact]
        public async Task SaveFileAsync_ValidJpeg_ReturnsUploadUrl()
        {
            var file   = MakeFile("photo.jpeg", "image/jpeg");
            var result = await CreateService().SaveFileAsync(file);

            result.Should().StartWith("/uploads/");
            result.Should().EndWith(".jpeg");
        }

        [Fact]
        public async Task SaveFileAsync_ValidDocx_ReturnsUploadUrl()
        {
            var file   = MakeFile("doc.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            var result = await CreateService().SaveFileAsync(file);

            result.Should().StartWith("/uploads/");
            result.Should().EndWith(".docx");
        }

        [Fact]
        public async Task SaveFileAsync_ValidXlsx_ReturnsUploadUrl()
        {
            var file   = MakeFile("sheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            var result = await CreateService().SaveFileAsync(file);

            result.Should().StartWith("/uploads/");
            result.Should().EndWith(".xlsx");
        }

        [Fact]
        public async Task SaveFileAsync_CreatesUploadsFolder_WhenNotExists()
        {
            // Use a path that doesn't have uploads subfolder yet
            var newTemp = Path.Combine(Path.GetTempPath(), $"NoUploads_{Guid.NewGuid()}");
            Directory.CreateDirectory(newTemp);
            _env.Setup(e => e.WebRootPath).Returns(newTemp);

            var file = MakeFile("test.pdf", "application/pdf");
            await CreateService().SaveFileAsync(file);

            Directory.Exists(Path.Combine(newTemp, "uploads")).Should().BeTrue();
            Directory.Delete(newTemp, recursive: true);
        }

        [Fact]
        public async Task SaveFileAsync_UsesGuidFileName_NotOriginalName()
        {
            var file   = MakeFile("myreceipt.pdf", "application/pdf");
            var result = await CreateService().SaveFileAsync(file);

            // Result should NOT contain the original filename
            result.Should().NotContain("myreceipt");
            // Should be a GUID-based name
            result.Should().MatchRegex(@"/uploads/[0-9a-f\-]{36}\.pdf");
        }

        [Fact]
        public async Task SaveFileAsync_ExtensionIsLowercased()
        {
            var file   = MakeFile("PHOTO.PNG", "image/png");
            var result = await CreateService().SaveFileAsync(file);

            result.Should().EndWith(".png"); // lowercase
        }

        // ── SaveFileAsync — validation failures ───────────────────────────────

        [Fact]
        public async Task SaveFileAsync_NullFile_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService().SaveFileAsync(null!));
        }

        [Fact]
        public async Task SaveFileAsync_EmptyFile_ThrowsArgumentException()
        {
            var file = MakeFile("empty.pdf", "application/pdf", length: 0);
            await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService().SaveFileAsync(file));
        }

        [Fact]
        public async Task SaveFileAsync_FileTooLarge_ThrowsArgumentException()
        {
            // 11 MB — exceeds 10 MB limit
            var file = MakeFile("big.pdf", "application/pdf", length: 11 * 1024 * 1024);
            var ex   = await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService().SaveFileAsync(file));

            ex.Message.Should().Contain("exceeds the maximum allowed size");
        }

        [Fact]
        public async Task SaveFileAsync_DisallowedExtension_ThrowsArgumentException()
        {
            var file = MakeFile("script.exe", "application/octet-stream");
            var ex   = await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService().SaveFileAsync(file));

            ex.Message.Should().Contain("not allowed");
        }

        [Fact]
        public async Task SaveFileAsync_DisallowedMimeType_ThrowsArgumentException()
        {
            // Extension is .pdf but MIME type is wrong
            var file = MakeFile("fake.pdf", "text/plain");
            var ex   = await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService().SaveFileAsync(file));

            ex.Message.Should().Contain("MIME type");
        }

        [Fact]
        public async Task SaveFileAsync_NoExtension_ThrowsArgumentException()
        {
            var file = MakeFile("noextension", "application/pdf");
            var ex   = await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService().SaveFileAsync(file));

            ex.Message.Should().Contain("not allowed");
        }

        [Fact]
        public async Task SaveFileAsync_TxtExtension_ThrowsArgumentException()
        {
            var file = MakeFile("notes.txt", "text/plain");
            await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService().SaveFileAsync(file));
        }

        [Fact]
        public async Task SaveFileAsync_ZipExtension_ThrowsArgumentException()
        {
            var file = MakeFile("archive.zip", "application/zip");
            await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService().SaveFileAsync(file));
        }

        // ── SaveFilesAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task SaveFilesAsync_NullList_ReturnsEmpty()
        {
            var result = await CreateService().SaveFilesAsync(null!);
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SaveFilesAsync_EmptyList_ReturnsEmpty()
        {
            var result = await CreateService().SaveFilesAsync(new List<IFormFile>());
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SaveFilesAsync_InvalidSingleFile_ThrowsArgumentException()
        {
            var files = new List<IFormFile>
            {
                MakeFile("invalid.exe", "application/octet-stream")
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService().SaveFilesAsync(files));
        }

        [Fact]
        public async Task SaveFilesAsync_SingleFile_ReturnsOneUrl()
        {
            var files = new List<IFormFile>
            {
                MakeFile("receipt.pdf", "application/pdf")
            };

            var result = await CreateService().SaveFilesAsync(files);

            result.Should().HaveCount(1);
            result.First().Should().StartWith("/uploads/");
        }

        // ── WebRootPath fallback ──────────────────────────────────────────────

        [Fact]
        public async Task SaveFileAsync_NullWebRootPath_FallsBackToCurrentDirectory()
        {
            _env.Setup(e => e.WebRootPath).Returns((string?)null);

            var file = MakeFile("test.pdf", "application/pdf");

            // Should not throw — falls back to Directory.GetCurrentDirectory()/wwwroot/uploads
            var result = await CreateService().SaveFileAsync(file);
            result.Should().StartWith("/uploads/");

            // Cleanup
            var fallbackPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (Directory.Exists(fallbackPath))
            {
                var fileName = result.Replace("/uploads/", "");
                var filePath = Path.Combine(fallbackPath, fileName);
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }
    }
}
