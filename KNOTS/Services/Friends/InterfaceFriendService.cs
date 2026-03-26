using KNOTS.Models;

namespace KNOTS.Services;

public interface InterfaceFriendService
{
    Task<List<FriendSearchResult>> SearchUsersAsync(string currentUsername, string searchTerm, int maxResults = 20);
    Task<(bool Success, string Message)> SendFriendRequestAsync(string senderUsername, string receiverUsername);
    Task<List<FriendRequest>> GetIncomingRequestsAsync(string username);
    Task<List<User>> GetFriendsAsync(string username);
    Task<(bool Success, string Message)> AcceptRequestAsync(int requestId, string currentUsername);
    Task<(bool Success, string Message)> DeclineRequestAsync(int requestId, string currentUsername);
    Task<int> DeleteExpiredPendingRequestsAsync(CancellationToken cancellationToken = default);
}
