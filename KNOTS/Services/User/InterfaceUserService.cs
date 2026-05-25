namespace KNOTS.Services;

public interface InterfaceUserService
{
    string? CurrentUser { get; }
    string? CurrentUserEmail { get; }
    bool IsAuthenticated { get; }
    bool IsCurrentUserBusiness { get; }
    event Action? OnAuthenticationChanged;
    
    (bool Success, string Message) RegisterUser(string username, string password);
    (bool Success, string Message) RegisterBusinessUser(string businessName, string email, string password);
    (bool Success, string Message) LoginUser(string username, string password);
    (bool Success, string Message) LoginBusinessUser(string email, string password);
    void LogoutUser();
    int GetTotalUsersCount();
    void UpdateUserStatistics(string username, double compatibilityScore, bool wasBestMatch);
    List<User> GetLeaderboard(int topCount = 10);
    int GetUserRank(string username);
}
