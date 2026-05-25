using Bunit;
using KNOTS.Components.Pages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using KNOTS.Services;
using KNOTS.Services.Interfaces;

public class GameTestBase : BunitContext
{
    protected (IRenderedComponent<Game>, Mock<NavigationManager>, Mock<InterfaceUserService>, Mock<InterfaceCompatibilityService>, Mock<IGameRoomService>) SetupGameComponent(
        bool isAuthenticated = true, 
        string currentUser = "TestUser")
    {
        var mockNav = new Mock<NavigationManager>();
        var mockUserService = new Mock<InterfaceUserService>();
        var mockCompatibility = new Mock<InterfaceCompatibilityService>();
        var mockGameRoom = new Mock<IGameRoomService>();
        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();

        mockUserService.Setup(u => u. IsAuthenticated).Returns(isAuthenticated);
        mockUserService.Setup(u => u.CurrentUser). Returns(currentUser);
        mockUserService.Setup(u => u.IsCurrentUserBusiness).Returns(false);
        mockUserService.Setup(u => u.CurrentUserEmail).Returns((string?)null);
        mockWebHostEnvironment.SetupGet(e => e.WebRootPath).Returns(Path.GetTempPath());

        Services.AddSingleton(mockUserService.Object);
        Services. AddSingleton(mockCompatibility.Object);
        Services.AddSingleton(mockGameRoom. Object);
        Services.AddSingleton(mockNav.Object);
        Services.AddSingleton(mockWebHostEnvironment.Object);

        var component = Render<Game>();

        return (component, mockNav, mockUserService, mockCompatibility, mockGameRoom);
    }
}
