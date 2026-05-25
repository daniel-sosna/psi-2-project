using System.Diagnostics.CodeAnalysis;
using KNOTS.Data;
using KNOTS.Models;
using Microsoft.EntityFrameworkCore;
using KNOTS.Exceptions;
using KNOTS.Services.Interfaces;
using System.Text.RegularExpressions;

namespace KNOTS.Services;

public class UserService : InterfaceUserService
{
    private readonly AppDbContext _context;
    private readonly InterfaceLoggingService _logger;
    
    public string? CurrentUser { get; private set; }
    public string? CurrentUserEmail { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentUser);
    public bool IsCurrentUserBusiness { get; private set; }
    public event Action? OnAuthenticationChanged;
    
    public UserService(AppDbContext context, InterfaceLoggingService logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    

    public (bool Success, string Message) RegisterUser(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Username and password cannot be empty.");
        if (username.Length < 3)
            throw new ArgumentException("Username must be at least 3 characters long.");
        if (password.Length < 4)
            throw new ArgumentException("Password must be at least 4 characters long.");
        
        try
        {
            var usernameLower = username.ToLower();
            if (_context.Users.Any(u => u.Username.ToLower() == usernameLower))
                throw new UserAlreadyExistsException(username);
            
            var passwordHash = PasswordHasher.Hash(password);
            var newUser = new User
            {
                Username = username,
                PasswordHash = passwordHash,
                UserType = UserType.Regular,
                CreatedAt = DateTime.Now,
                TotalGamesPlayed = 0,
                BestMatchesCount = 0,
                AverageCompatibilityScore = 0.0
            };
            
            _context.Users.Add(newUser);
            _context.SaveChanges();
            return (true, "Registration successful! You can now log in.");
        }
        catch (UserAlreadyExistsException ex)
        {
            _logger.LogException(ex, $"User with this username already exists: {username}");
            return (false, ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogException(ex, $"Database error for user: {username}");
            return (false, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogException(ex, $"Operation error for user: {username}");
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"Unexpected error for user: {username}");
            return (false, ex.Message);
        }
    }

    public (bool Success, string Message) RegisterBusinessUser(string businessName, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(businessName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Business name, email, and password cannot be empty.");
        if (businessName.Length < 3)
            throw new ArgumentException("Business name must be at least 3 characters long.");
        if (!IsValidEmail(email))
            throw new ArgumentException("Please enter a valid email address.");
        if (password.Length < 4)
            throw new ArgumentException("Password must be at least 4 characters long.");

        try
        {
            var businessNameLower = businessName.ToLower();
            var emailLower = email.ToLower();

            if (_context.Users.Any(u => u.Username.ToLower() == businessNameLower))
                throw new UserAlreadyExistsException(businessName);

            if (_context.Users.Any(u => u.Email != null && u.Email.ToLower() == emailLower))
                return (false, "A business user with this email already exists.");

            var passwordHash = PasswordHasher.Hash(password);
            var newUser = new User
            {
                Username = businessName,
                Email = email,
                UserType = UserType.Business,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.Now,
                TotalGamesPlayed = 0,
                BestMatchesCount = 0,
                AverageCompatibilityScore = 0.0
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();
            return (true, "Business registration successful! You can now log in.");
        }
        catch (UserAlreadyExistsException ex)
        {
            _logger.LogException(ex, $"Business with this name already exists: {businessName}");
            return (false, ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogException(ex, $"Database error for business user: {businessName}");
            return (false, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogException(ex, $"Operation error for business user: {businessName}");
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"Unexpected error for business user: {businessName}");
            return (false, ex.Message);
        }
    }

    [ExcludeFromCodeCoverage]
    public (bool Success, string Message) LoginUser(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Username and password cannot be empty");

        try
        {
            var user = _context.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());

            if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
                throw new InvalidCredentialsException("Invalid username or password");
            
            SetAuthenticatedUser(user);
            return (true, "Login successful");
        }
        catch (ArgumentException ex)
        {
            _logger.LogException(ex, "Username and password cannot be empty");
            return (false, ex.Message);
        }
        catch (InvalidCredentialsException ex)
        {
            _logger.LogException(ex, $"Invalid username or password: {username}");
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"Unexpected error during login: {username}");
            return (false, ex.Message);
        }
    }

    [ExcludeFromCodeCoverage]
    public (bool Success, string Message) LoginBusinessUser(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Email and password cannot be empty");

        try
        {
            var user = _context.Users.FirstOrDefault(u =>
                u.UserType == UserType.Business &&
                u.Email != null &&
                u.Email.ToLower() == email.ToLower());

            if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
                throw new InvalidCredentialsException("Invalid email or password");

            SetAuthenticatedUser(user);
            return (true, "Login successful");
        }
        catch (ArgumentException ex)
        {
            _logger.LogException(ex, "Email and password cannot be empty");
            return (false, ex.Message);
        }
        catch (InvalidCredentialsException ex)
        {
            _logger.LogException(ex, $"Invalid business email or password: {email}");
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"Unexpected error during business login: {email}");
            return (false, ex.Message);
        }
    }
    
    public void LogoutUser()
    {
        CurrentUser = null;
        CurrentUserEmail = null;
        IsCurrentUserBusiness = false;
        OnAuthenticationChanged?.Invoke();
    }
    
    public int GetTotalUsersCount() => _context.Users.Count();
    
    public void UpdateUserStatistics(string username, double compatibilityScore, bool wasBestMatch)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
        if (user == null) throw new UserNotFoundException(username);

        try
        {
            user.TotalGamesPlayed++;
            if (wasBestMatch) user.BestMatchesCount++;

            user.AverageCompatibilityScore =
                ((user.AverageCompatibilityScore * (user.TotalGamesPlayed - 1)) + compatibilityScore)
                / user.TotalGamesPlayed;

            _context.SaveChanges();
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogException(ex, $"User with this username doesn't exist: {username}");
        }
    }
    
    public List<User> GetLeaderboard(int topCount = 10)
    {
        var leaderboard = new Leaderboard<User>(_context.Users.AsEnumerable());
        return leaderboard
            .SelectTop(topCount, user => user)
            .ToList();
    }
    
    public int GetUserRank(string username)
    {
        var sortedUsers = _context.Users
            .AsEnumerable()
            .OrderByDescending(u => u)
            .ToList();
        var rank = sortedUsers.FindIndex(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)) + 1;
        return rank;
    }

    private void SetAuthenticatedUser(User user)
    {
        CurrentUser = user.Username;
        CurrentUserEmail = user.Email;
        IsCurrentUserBusiness = user.UserType == UserType.Business;
        OnAuthenticationChanged?.Invoke();
    }

    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
