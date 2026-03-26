using KNOTS.Models;

namespace KNOTS.testing.FriendServiceTests;

public class SendFriendRequest_CreatesPendingRequest : FriendServiceTestBase
{
    [Fact]
    public async Task CreatesPendingRequest()
    {
        AddUser("Alice");
        AddUser("Bob");

        var result = await FriendService.SendFriendRequestAsync("Alice", "Bob");

        Assert.True(result.Success);
        var request = Assert.Single(Context.FriendRequests);
        Assert.Equal("Alice", request.RequesterUsername);
        Assert.Equal("Bob", request.ReceiverUsername);
        Assert.Equal(FriendRequestStatus.Pending, request.Status);
    }
}
