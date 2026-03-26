using KNOTS.Data;
using KNOTS.Services;
using Microsoft.EntityFrameworkCore;

namespace KNOTS.testing.FriendServiceTests;

public abstract class FriendServiceTestBase : IDisposable
{
    protected readonly AppDbContext Context;
    protected readonly LoggingService Logger;
    protected readonly FriendService FriendService;

    protected FriendServiceTestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Context = new AppDbContext(options);
        Logger = new LoggingService();
        FriendService = new FriendService(Context, Logger);
    }

    protected void AddUser(string username)
    {
        Context.Users.Add(new User
        {
            Username = username,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });
        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}
