using KNOTS.Models;

namespace KNOTS.testing.FriendServiceTests;

public class AcceptRequest_CreatesFriendshipAndPreventsResend : FriendServiceTestBase
{
    [Fact]
    public async Task AcceptsRequestAndTreatsUsersAsFriends()
    {
        AddUser("Alice");
        AddUser("Bob");

        await FriendService.SendFriendRequestAsync("Alice", "Bob");
        var request = Assert.Single(Context.FriendRequests);

        var accept = await FriendService.AcceptRequestAsync(request.Id, "Bob");
        var friends = await FriendService.GetFriendsAsync("Alice");
        var resend = await FriendService.SendFriendRequestAsync("Bob", "Alice");

        Assert.True(accept.Success);
        Assert.Equal(FriendRequestStatus.Accepted, request.Status);
        Assert.Single(friends);
        Assert.Equal("Bob", friends[0].Username);
        Assert.False(resend.Success);
    }
}
