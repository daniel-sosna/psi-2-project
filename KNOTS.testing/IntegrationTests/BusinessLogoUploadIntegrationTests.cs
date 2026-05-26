using System.Reflection;
using Bunit;
using KNOTS.Components.Pages;
using KNOTS.Services;
using KNOTS.Services.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KNOTS.testing.IntegrationTests;

public class BusinessLogoUploadIntegrationTests : BunitContext, IDisposable
{
    private readonly string _webRootPath = Path.Combine(Path.GetTempPath(), $"knots-logo-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task BusinessLogoUpload_ValidImage_SavesFileAndShowsSelectedFileName()
    {
        var component = RenderBusinessGameComponent();
        var file = new TestBrowserFile(
            "brand-logo.png",
            "image/png",
            "fake image bytes"u8.ToArray());

        await InvokeLogoUpload(component, file);

        var uploadsDirectory = Path.Combine(_webRootPath, "uploads", "business-logos");
        var savedFiles = Directory.Exists(uploadsDirectory)
            ? Directory.GetFiles(uploadsDirectory)
            : Array.Empty<string>();

        Assert.Single(savedFiles);
        Assert.EndsWith(".png", savedFiles[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Selected: brand-logo.png", component.Markup);
        Assert.Contains("Logo 'brand-logo.png' attached to the room.", component.Markup);
    }

    [Fact]
    public async Task BusinessLogoUpload_InvalidFileType_ShowsErrorAndDoesNotSaveFile()
    {
        var component = RenderBusinessGameComponent();
        var file = new TestBrowserFile(
            "notes.txt",
            "text/plain",
            "not an image"u8.ToArray());

        await InvokeLogoUpload(component, file);

        var uploadsDirectory = Path.Combine(_webRootPath, "uploads", "business-logos");
        var savedFiles = Directory.Exists(uploadsDirectory)
            ? Directory.GetFiles(uploadsDirectory)
            : Array.Empty<string>();

        Assert.Empty(savedFiles);
        Assert.Equal("Error: Please upload an image file for the business logo.", component.Instance.statusMessage);
        Assert.DoesNotContain("Selected: notes.txt", component.Markup);
    }

    [Fact]
    public async Task BusinessLogoUpload_FileTooLarge_ShowsSizeLimitInMegabytes()
    {
        var component = RenderBusinessGameComponent();
        var file = new TestBrowserFile(
            "huge-logo.png",
            "image/png",
            new byte[(2 * 1024 * 1024) + 1]);

        await InvokeLogoUpload(component, file);

        var uploadsDirectory = Path.Combine(_webRootPath, "uploads", "business-logos");
        var savedFiles = Directory.Exists(uploadsDirectory)
            ? Directory.GetFiles(uploadsDirectory)
            : Array.Empty<string>();

        Assert.Empty(savedFiles);
        Assert.Equal("Error: Logo file must be 2 MB or smaller.", component.Instance.statusMessage);
        Assert.DoesNotContain("2097152", component.Instance.statusMessage);
    }

    public new void Dispose()
    {
        base.Dispose();

        if (Directory.Exists(_webRootPath))
        {
            Directory.Delete(_webRootPath, recursive: true);
        }
    }

    private IRenderedComponent<Game> RenderBusinessGameComponent()
    {
        Directory.CreateDirectory(_webRootPath);

        var mockUserService = new Mock<InterfaceUserService>();
        mockUserService.SetupGet(service => service.IsAuthenticated).Returns(true);
        mockUserService.SetupGet(service => service.CurrentUser).Returns("Business Owner");
        mockUserService.SetupGet(service => service.IsCurrentUserBusiness).Returns(true);
        mockUserService.SetupGet(service => service.CurrentUserEmail).Returns("owner@coffee.test");

        var mockCompatibilityService = new Mock<InterfaceCompatibilityService>();
        var mockGameRoomService = new Mock<IGameRoomService>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        mockEnvironment.SetupGet(environment => environment.WebRootPath).Returns(_webRootPath);

        Services.AddSingleton(mockUserService.Object);
        Services.AddSingleton(mockCompatibilityService.Object);
        Services.AddSingleton(mockGameRoomService.Object);
        Services.AddSingleton(mockEnvironment.Object);

        JSInterop.SetupVoid("eval", _ => true);
        JSInterop.SetupVoid("setBlazorGameComponent", _ => true);
        JSInterop.Setup<bool>("initializeGameConnection", _ => true).SetResult(false);

        return Render<Game>();
    }

    private static async Task InvokeLogoUpload(IRenderedComponent<Game> component, IBrowserFile file)
    {
        var method = typeof(Game).GetMethod("HandleBusinessLogoUpload", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        await component.InvokeAsync(async () =>
        {
            var task = method!.Invoke(component.Instance, new object[] { new InputFileChangeEventArgs(new[] { file }) }) as Task;
            Assert.NotNull(task);
            await task!;
        });
    }

    private sealed class TestBrowserFile : IBrowserFile
    {
        private readonly byte[] _content;

        public TestBrowserFile(string name, string contentType, byte[] content)
        {
            Name = name;
            ContentType = contentType;
            _content = content;
            LastModified = DateTimeOffset.UtcNow;
            Size = content.Length;
        }

        public string Name { get; }
        public DateTimeOffset LastModified { get; }
        public long Size { get; }
        public string ContentType { get; }

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            if (Size > maxAllowedSize)
            {
                throw new IOException("File exceeds maximum allowed size.");
            }

            return new MemoryStream(_content, writable: false);
        }
    }
}
