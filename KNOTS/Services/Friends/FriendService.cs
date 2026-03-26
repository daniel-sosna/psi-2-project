using KNOTS.Data;
using KNOTS.Models;
using KNOTS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KNOTS.Services;

public class FriendService : InterfaceFriendService
{
    private readonly AppDbContext _context;
    private readonly InterfaceLoggingService _logger;

    public FriendService(AppDbContext context, InterfaceLoggingService logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<FriendSearchResult>> SearchUsersAsync(string currentUsername, string searchTerm, int maxResults = 20)
    {
        var normalizedCurrent = Normalize(currentUsername);
        var term = Normalize(searchTerm);

        if (string.IsNullOrWhiteSpace(normalizedCurrent) || string.IsNullOrWhiteSpace(term))
        {
            return new List<FriendSearchResult>();
        }

        var matchedUsers = await _context.Users
            .Where(u => u.Username.ToLower().Contains(term) && u.Username.ToLower() != normalizedCurrent)
            .OrderBy(u => u.Username)
            .Take(Math.Max(1, maxResults))
            .ToListAsync();

        if (matchedUsers.Count == 0)
        {
            return new List<FriendSearchResult>();
        }

        var pairKeys = matchedUsers
            .Select(user => BuildPairKey(currentUsername, user.Username))
            .Distinct()
            .ToList();

        var existingRelations = await _context.FriendRequests
            .Where(request => pairKeys.Contains(request.PairKey))
            .ToListAsync();

        return matchedUsers
            .Select(user => BuildSearchResult(currentUsername, user.Username, existingRelations))
            .ToList();
    }

    public async Task<(bool Success, string Message)> SendFriendRequestAsync(string senderUsername, string receiverUsername)
    {
        if (string.IsNullOrWhiteSpace(senderUsername) || string.IsNullOrWhiteSpace(receiverUsername))
        {
            return (false, "Both usernames are required.");
        }

        if (Normalize(senderUsername) == Normalize(receiverUsername))
        {
            return (false, "You cannot send a friend request to yourself.");
        }

        try
        {
            var sender = await FindUserAsync(senderUsername);
            var receiver = await FindUserAsync(receiverUsername);

            if (sender == null || receiver == null)
            {
                return (false, "That user could not be found.");
            }

            var existing = await GetRelationshipAsync(sender.Username, receiver.Username);
            if (existing != null)
            {
                return existing.Status switch
                {
                    FriendRequestStatus.Accepted => (false, "You are already friends."),
                    FriendRequestStatus.Pending when Normalize(existing.RequesterUsername) == Normalize(sender.Username)
                        => (false, "You have already sent a friend request to this user."),
                    FriendRequestStatus.Pending => (false, "This user has already sent you a friend request."),
                    _ => (false, "A friend request already exists for this user.")
                };
            }

            var friendRequest = new FriendRequest
            {
                RequesterUsername = sender.Username,
                ReceiverUsername = receiver.Username,
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                PairKey = BuildPairKey(sender.Username, receiver.Username)
            };

            _context.FriendRequests.Add(friendRequest);
            await _context.SaveChangesAsync();
            return (true, $"Friend request sent to {receiver.Username}.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogException(ex, $"Failed to save friend request from {senderUsername} to {receiverUsername}");
            return (false, "We couldn't send that friend request right now.");
        }
    }

    public async Task<List<FriendRequest>> GetIncomingRequestsAsync(string username)
    {
        var normalizedUsername = Normalize(username);

        return await _context.FriendRequests
            .Where(request => request.Status == FriendRequestStatus.Pending &&
                              request.ReceiverUsername.ToLower() == normalizedUsername)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<User>> GetFriendsAsync(string username)
    {
        var normalizedUsername = Normalize(username);

        var acceptedRequests = await _context.FriendRequests
            .Where(request => request.Status == FriendRequestStatus.Accepted &&
                              (request.RequesterUsername.ToLower() == normalizedUsername ||
                               request.ReceiverUsername.ToLower() == normalizedUsername))
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync();

        var friendUsernames = acceptedRequests
            .Select(request => Normalize(request.RequesterUsername) == normalizedUsername
                ? request.ReceiverUsername
                : request.RequesterUsername)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (friendUsernames.Count == 0)
        {
            return new List<User>();
        }

        return await _context.Users
            .Where(user => friendUsernames.Contains(user.Username))
            .OrderBy(user => user.Username)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> AcceptRequestAsync(int requestId, string currentUsername)
    {
        var friendRequest = await _context.FriendRequests.FirstOrDefaultAsync(request => request.Id == requestId);
        if (friendRequest == null || friendRequest.Status != FriendRequestStatus.Pending)
        {
            return (false, "That friend request is no longer available.");
        }

        if (Normalize(friendRequest.ReceiverUsername) != Normalize(currentUsername))
        {
            return (false, "You can only accept your own incoming requests.");
        }

        friendRequest.Status = FriendRequestStatus.Accepted;
        await _context.SaveChangesAsync();
        return (true, $"{friendRequest.RequesterUsername} is now your friend.");
    }

    public async Task<(bool Success, string Message)> DeclineRequestAsync(int requestId, string currentUsername)
    {
        var friendRequest = await _context.FriendRequests.FirstOrDefaultAsync(request => request.Id == requestId);
        if (friendRequest == null || friendRequest.Status != FriendRequestStatus.Pending)
        {
            return (false, "That friend request is no longer available.");
        }

        if (Normalize(friendRequest.ReceiverUsername) != Normalize(currentUsername))
        {
            return (false, "You can only decline your own incoming requests.");
        }

        _context.FriendRequests.Remove(friendRequest);
        await _context.SaveChangesAsync();
        return (true, "Friend request declined.");
    }

    public async Task<int> DeleteExpiredPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var expiredRequests = await _context.FriendRequests
            .Where(request => request.Status == FriendRequestStatus.Pending && request.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (expiredRequests.Count == 0)
        {
            return 0;
        }

        _context.FriendRequests.RemoveRange(expiredRequests);
        await _context.SaveChangesAsync(cancellationToken);
        return expiredRequests.Count;
    }

    private FriendSearchResult BuildSearchResult(string currentUsername, string candidateUsername, List<FriendRequest> relations)
    {
        var existing = relations.FirstOrDefault(request =>
            request.PairKey == BuildPairKey(currentUsername, candidateUsername));

        if (existing == null)
        {
            return new FriendSearchResult
            {
                Username = candidateUsername,
                CanSendRequest = true,
                StatusLabel = "Ready to connect"
            };
        }

        var normalizedCurrent = Normalize(currentUsername);
        var isRequester = Normalize(existing.RequesterUsername) == normalizedCurrent;

        return new FriendSearchResult
        {
            Username = candidateUsername,
            CanSendRequest = false,
            IsFriend = existing.Status == FriendRequestStatus.Accepted,
            HasPendingOutgoingRequest = existing.Status == FriendRequestStatus.Pending && isRequester,
            HasPendingIncomingRequest = existing.Status == FriendRequestStatus.Pending && !isRequester,
            StatusLabel = existing.Status switch
            {
                FriendRequestStatus.Accepted => "Already friends",
                FriendRequestStatus.Pending when isRequester => "Request sent",
                FriendRequestStatus.Pending => "Sent you a request",
                _ => string.Empty
            }
        };
    }

    private async Task<User?> FindUserAsync(string username)
    {
        var normalizedUsername = Normalize(username);
        return await _context.Users.FirstOrDefaultAsync(user => user.Username.ToLower() == normalizedUsername);
    }

    private async Task<FriendRequest?> GetRelationshipAsync(string username1, string username2)
    {
        var pairKey = BuildPairKey(username1, username2);
        return await _context.FriendRequests.FirstOrDefaultAsync(request => request.PairKey == pairKey);
    }

    private static string BuildPairKey(string username1, string username2)
    {
        var pair = new[] { Normalize(username1), Normalize(username2) };
        Array.Sort(pair, StringComparer.Ordinal);
        return string.Join("|", pair);
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();
}
