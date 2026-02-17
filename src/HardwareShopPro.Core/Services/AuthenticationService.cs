using HardwareShopPro.Core.Enums;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using Serilog;

namespace HardwareShopPro.Core.Services;

/// <summary>
/// Authentication service using BCrypt for password hashing.
/// Provides login, user creation, and session management.
/// </summary>
public class AuthenticationService
{
    private readonly IUserRepository _userRepo;
    private readonly IAuditLogRepository _auditRepo;
    private static readonly ILogger Logger = Log.ForContext<AuthenticationService>();

    /// <summary>
    /// Currently logged-in user. Null if not authenticated.
    /// </summary>
    public User? CurrentUser { get; private set; }

    /// <summary>
    /// Last activity timestamp for auto-logout feature.
    /// </summary>
    public DateTime LastActivity { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Auto-logout after this duration of inactivity.
    /// </summary>
    public TimeSpan InactivityTimeout { get; set; } = TimeSpan.FromMinutes(30);

    public bool IsAuthenticated => CurrentUser != null;
    public bool IsAdmin => CurrentUser?.Role == UserRole.Admin;
    public bool IsManagerOrAbove => CurrentUser?.Role >= UserRole.Manager;

    public AuthenticationService(IUserRepository userRepo, IAuditLogRepository auditRepo)
    {
        _userRepo = userRepo;
        _auditRepo = auditRepo;
    }

    /// <summary>
    /// Attempts to authenticate a user with the given credentials.
    /// </summary>
    /// <returns>Authenticated User, or null if login fails.</returns>
    public async Task<User?> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null || !user.IsActive)
        {
            Logger.Warning("Login failed for username: {Username}", username);
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            Logger.Warning("Invalid password for user: {Username}", username);
            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = user.Id,
                Username = user.Username,
                Action = "LOGIN_FAILED",
                Entity = "User",
                EntityId = user.Id,
                Details = "Invalid password attempt"
            });
            return null;
        }

        CurrentUser = user;
        LastActivity = DateTime.UtcNow;
        await _userRepo.UpdateLastLoginAsync(user.Id);

        await _auditRepo.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Username = user.Username,
            Action = "LOGIN_SUCCESS",
            Entity = "User",
            EntityId = user.Id,
            Details = $"Role: {user.Role}"
        });

        Logger.Information("User {Username} logged in successfully (Role: {Role})", user.Username, user.Role);
        return user;
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    public async Task LogoutAsync()
    {
        if (CurrentUser != null)
        {
            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = CurrentUser.Id,
                Username = CurrentUser.Username,
                Action = "LOGOUT",
                Entity = "User",
                EntityId = CurrentUser.Id
            });
            Logger.Information("User {Username} logged out", CurrentUser.Username);
        }
        CurrentUser = null;
    }

    /// <summary>
    /// Creates a new user with BCrypt password hashing.
    /// Only Admin role can create users.
    /// </summary>
    public async Task<int> CreateUserAsync(string username, string password, string displayName, UserRole role)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        var user = new User
        {
            Username = username,
            PasswordHash = hash,
            DisplayName = displayName,
            Role = role,
            IsActive = true
        };

        var id = await _userRepo.AddAsync(user);

        if (CurrentUser != null)
        {
            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = CurrentUser.Id,
                Username = CurrentUser.Username,
                Action = "CREATE_USER",
                Entity = "User",
                EntityId = id,
                Details = $"Created user '{username}' with role {role}"
            });
        }

        return id;
    }

    /// <summary>
    /// Records user activity to reset the inactivity timer.
    /// </summary>
    public void RecordActivity() => LastActivity = DateTime.UtcNow;

    /// <summary>
    /// Checks if the session has expired due to inactivity.
    /// </summary>
    public bool IsSessionExpired => IsAuthenticated &&
        (DateTime.UtcNow - LastActivity) > InactivityTimeout;

    /// <summary>
    /// Checks if current user has the required role.
    /// </summary>
    public bool HasRole(UserRole minimumRole)
    {
        return CurrentUser != null && CurrentUser.Role >= minimumRole;
    }
}
