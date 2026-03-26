namespace KNOTS.testing.FriendServiceTests;

public class SendFriendRequest_PreventsDuplicateAndReversePendingRequests : FriendServiceTestBase
{
    [Fact]
    public async Task RejectsDuplicateAndReversePendingRequests()
    {
        AddUser("Alice");
        AddUser("Bob");

        var first = await FriendService.SendFriendRequestAsync("Alice", "Bob");
        var duplicate = await FriendService.SendFriendRequestAsync("Alice", "Bob");
        var reverse = await FriendService.SendFriendRequestAsync("Bob", "Alice");

        Assert.True(first.Success);
        Assert.False(duplicate.Success);
        Assert.False(reverse.Success);
        Assert.Single(Context.FriendRequests);
    }
}
