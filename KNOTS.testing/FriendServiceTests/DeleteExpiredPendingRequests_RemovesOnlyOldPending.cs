using KNOTS.Models;

namespace KNOTS.testing.FriendServiceTests;

public class DeleteExpiredPendingRequests_RemovesOnlyOldPending : FriendServiceTestBase
{
    [Fact]
    public async Task DeletesOnlyExpiredPendingRequests()
    {
        AddUser("Alice");
        AddUser("Bob");
        AddUser("Cara");

        Context.FriendRequests.AddRange(
            new FriendRequest
            {
                RequesterUsername = "Alice",
                ReceiverUsername = "Bob",
                PairKey = "alice|bob",
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            },
            new FriendRequest
            {
                RequesterUsername = "Alice",
                ReceiverUsername = "Cara",
                PairKey = "alice|cara",
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new FriendRequest
            {
                RequesterUsername = "Bob",
                ReceiverUsername = "Cara",
                PairKey = "bob|cara",
                Status = FriendRequestStatus.Accepted,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            });
        Context.SaveChanges();

        var deletedCount = await FriendService.DeleteExpiredPendingRequestsAsync();

        Assert.Equal(1, deletedCount);
        Assert.Equal(2, Context.FriendRequests.Count());
        Assert.DoesNotContain(Context.FriendRequests, request => request.PairKey == "alice|bob");
    }
}
