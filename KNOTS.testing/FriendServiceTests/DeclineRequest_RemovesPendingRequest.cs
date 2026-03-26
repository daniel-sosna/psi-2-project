namespace KNOTS.testing.FriendServiceTests;

public class DeclineRequest_RemovesPendingRequest : FriendServiceTestBase
{
    [Fact]
    public async Task DeclinesRequestAndDeletesIt()
    {
        AddUser("Alice");
        AddUser("Bob");

        await FriendService.SendFriendRequestAsync("Alice", "Bob");
        var request = Assert.Single(Context.FriendRequests);

        var decline = await FriendService.DeclineRequestAsync(request.Id, "Bob");

        Assert.True(decline.Success);
        Assert.Empty(Context.FriendRequests);
    }
}
