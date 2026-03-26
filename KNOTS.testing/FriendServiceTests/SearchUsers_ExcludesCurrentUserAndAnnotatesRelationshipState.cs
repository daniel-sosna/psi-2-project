namespace KNOTS.testing.FriendServiceTests;

public class SearchUsers_ExcludesCurrentUserAndAnnotatesRelationshipState : FriendServiceTestBase
{
    [Fact]
    public async Task SearchReflectsRelationshipState()
    {
        AddUser("Alice");
        AddUser("Bob");
        AddUser("Bobby");
        AddUser("Cara");

        await FriendService.SendFriendRequestAsync("Alice", "Bob");
        await FriendService.SendFriendRequestAsync("Cara", "Alice");
        var incoming = Assert.Single(Context.FriendRequests.Where(request => request.RequesterUsername == "Cara"));
        await FriendService.AcceptRequestAsync(incoming.Id, "Alice");

        var results = await FriendService.SearchUsersAsync("Alice", "b");

        Assert.DoesNotContain(results, result => result.Username == "Alice");

        var bob = Assert.Single(results, result => result.Username == "Bob");
        Assert.True(bob.HasPendingOutgoingRequest);
        Assert.False(bob.CanSendRequest);

        var bobby = Assert.Single(results, result => result.Username == "Bobby");
        Assert.True(bobby.CanSendRequest);

        var caraResults = await FriendService.SearchUsersAsync("Alice", "car");
        var cara = Assert.Single(caraResults);
        Assert.True(cara.IsFriend);
        Assert.False(cara.CanSendRequest);
    }
}
