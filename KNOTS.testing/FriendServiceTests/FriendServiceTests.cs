using KNOTS.Data;
using KNOTS.Models;
using KNOTS.Services;
using Microsoft.EntityFrameworkCore;

namespace KNOTS.testing.FriendServiceTests;

public class FriendServiceTests : IDisposable
{
    private readonly AppDbContext Context;
    private readonly LoggingService Logger;
    private readonly FriendService FriendService;

    public FriendServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Context = new AppDbContext(options);
        Logger = new LoggingService();
        FriendService = new FriendService(Context, Logger);
    }

    private void AddUser(string username)
    {
        Context.Users.Add(new User
        {
            Username = username,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });
        Context.SaveChanges();
    }

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

    [Fact]
    public async Task RejectsSelfRequest()
    {
        AddUser("Alice");

        var result = await FriendService.SendFriendRequestAsync("Alice", "Alice");

        Assert.False(result.Success);
        Assert.Empty(Context.FriendRequests);
    }

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
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            },
            new FriendRequest
            {
                RequesterUsername = "Alice",
                ReceiverUsername = "Cara",
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new FriendRequest
            {
                RequesterUsername = "Bob",
                ReceiverUsername = "Cara",
                Status = FriendRequestStatus.Accepted,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            });
        Context.SaveChanges();

        var deletedCount = await FriendService.DeleteExpiredPendingRequestsAsync();

        Assert.Equal(1, deletedCount);
        Assert.Equal(2, Context.FriendRequests.Count());
        Assert.DoesNotContain(Context.FriendRequests, request =>
            request.RequesterUsername == "Alice" && request.ReceiverUsername == "Bob");
    }

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

        var bobResults = await FriendService.SearchUsersAsync("Alice", "Bob");
        Assert.DoesNotContain(bobResults, result => result.Username == "Alice");

        var bob = Assert.Single(bobResults);
        Assert.Equal("Bob", bob.Username);
        Assert.True(bob.HasPendingOutgoingRequest);
        Assert.False(bob.CanSendRequest);

        var bobbyResults = await FriendService.SearchUsersAsync("Alice", "Bobby");
        var bobby = Assert.Single(bobbyResults);
        Assert.Equal("Bobby", bobby.Username);
        Assert.True(bobby.CanSendRequest);

        var caraResults = await FriendService.SearchUsersAsync("Alice", "Cara");
        var cara = Assert.Single(caraResults);
        Assert.True(cara.IsFriend);
        Assert.False(cara.CanSendRequest);
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}
