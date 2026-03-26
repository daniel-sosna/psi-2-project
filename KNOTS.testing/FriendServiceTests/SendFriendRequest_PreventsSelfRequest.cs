namespace KNOTS.testing.FriendServiceTests;

public class SendFriendRequest_PreventsSelfRequest : FriendServiceTestBase
{
    [Fact]
    public async Task RejectsSelfRequest()
    {
        AddUser("Alice");

        var result = await FriendService.SendFriendRequestAsync("Alice", "Alice");

        Assert.False(result.Success);
        Assert.Empty(Context.FriendRequests);
    }
}
