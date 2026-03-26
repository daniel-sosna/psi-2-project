using Bunit;
using KNOTS.Components.Pages;
using KNOTS.Models;
using KNOTS.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KNOTS.testing.PagesTests.HomeTests;

public class FriendsTabTests : BunitContext
{
    private readonly Mock<InterfaceUserService> _mockUserService = new();
    private readonly Mock<InterfaceFriendService> _mockFriendService = new();

    public FriendsTabTests()
    {
        _mockUserService.Setup(service => service.IsAuthenticated).Returns(true);
        _mockUserService.Setup(service => service.CurrentUser).Returns("Alice");
        _mockFriendService.Setup(service => service.GetIncomingRequestsAsync("Alice"))
            .ReturnsAsync(new List<FriendRequest>
            {
                new() { Id = 7, RequesterUsername = "Bob", ReceiverUsername = "Alice", CreatedAt = DateTime.UtcNow }
            });
        _mockFriendService.Setup(service => service.GetFriendsAsync("Alice"))
            .ReturnsAsync(new List<User> { new() { Username = "Cara" } });
        _mockFriendService.Setup(service => service.SearchUsersAsync("Alice", "bo", 20))
            .ReturnsAsync(new List<FriendSearchResult>
            {
                new() { Username = "Bob", CanSendRequest = true, StatusLabel = "Ready to connect" }
            });
        _mockFriendService.Setup(service => service.SendFriendRequestAsync("Alice", "Bob"))
            .ReturnsAsync((true, "Friend request sent to Bob."));

        Services.AddSingleton(_mockUserService.Object);
        Services.AddSingleton(_mockFriendService.Object);
    }

    [Fact]
    public async Task FriendsTab_SearchAndSendRequest_UsesFriendService()
    {
        var cut = Render<FriendsTab>();

        cut.Find(".friends-search-input").Input("bo");
        await cut.Find(".friends-search-button").ClickAsync(new MouseEventArgs());

        Assert.Contains("Bob", cut.Markup);

        await cut.Find(".friend-action-button").ClickAsync(new MouseEventArgs());

        _mockFriendService.Verify(service => service.SearchUsersAsync("Alice", "bo", 20), Times.AtLeastOnce);
        _mockFriendService.Verify(service => service.SendFriendRequestAsync("Alice", "Bob"), Times.Once);
    }
}
