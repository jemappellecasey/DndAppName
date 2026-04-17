using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using DndApp.Api.Data;

namespace DndApp.Api.Auth;

public interface ILocalAuthService
{
    Task<LocalSession> RegisterAsync(LocalRegisterRequest request, CancellationToken cancellationToken);
    Task<LocalSession> LoginAsync(LocalLoginRequest request, CancellationToken cancellationToken);
    Task<LocalSession?> GetSessionAsync(string sessionToken, CancellationToken cancellationToken);
}

public sealed class LocalAuthService : ILocalAuthService
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromDays(30);
    private readonly AppDbContext _db;

    public LocalAuthService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LocalSession> RegisterAsync(LocalRegisterRequest request, CancellationToken cancellationToken)
    {
        var userName = NormalizeUserNameInput(request.UserName);
        if (request.Password.Length < 8)
        {
            throw new InvalidOperationException("Password must be at least 8 characters.");
        }

        var normalized = NormalizeUserNameLookup(userName);
        var existing = await _db.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NormalizedUserName == normalized, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("A user with that name already exists.");
        }

        var user = new UserAccountEntity
        {
            UserId = $"local:{Guid.NewGuid():N}",
            UserName = userName,
            NormalizedUserName = normalized,
            PasswordHash = HashPassword(request.Password),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _db.UserAccounts.Add(user);
        var session = CreateSession(user.UserId, user.UserName);
        _db.UserSessions.Add(new UserSessionEntity
        {
            SessionToken = session.SessionToken,
            UserId = user.UserId,
            CreatedAtUtc = session.CreatedAtUtc,
            ExpiresAtUtc = session.ExpiresAtUtc
        });

        await _db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<LocalSession> LoginAsync(LocalLoginRequest request, CancellationToken cancellationToken)
    {
        var userName = NormalizeUserNameInput(request.UserName);
        var normalized = NormalizeUserNameLookup(userName);
        var user = await _db.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NormalizedUserName == normalized, cancellationToken);
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid username or password.");
        }

        var session = CreateSession(user.UserId, user.UserName);
        _db.UserSessions.Add(new UserSessionEntity
        {
            SessionToken = session.SessionToken,
            UserId = user.UserId,
            CreatedAtUtc = session.CreatedAtUtc,
            ExpiresAtUtc = session.ExpiresAtUtc
        });

        await _db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<LocalSession?> GetSessionAsync(string sessionToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        var token = sessionToken.Trim();
        var session = await _db.UserSessions
            .AsNoTracking()
            .Join(_db.UserAccounts.AsNoTracking(),
                s => s.UserId,
                u => u.UserId,
                (s, u) => new { Session = s, User = u })
            .FirstOrDefaultAsync(x => x.Session.SessionToken == token, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (session.Session.RevokedAtUtc is not null || session.Session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return new LocalSession(
            session.Session.SessionToken,
            session.User.UserId,
            session.User.UserName,
            session.Session.CreatedAtUtc,
            session.Session.ExpiresAtUtc);
    }

    private static string NormalizeUserNameInput(string userName)
    {
        var trimmed = string.IsNullOrWhiteSpace(userName) ? string.Empty : userName.Trim();
        if (trimmed.Length is < 3 or > 40)
        {
            throw new InvalidOperationException("Username must be between 3 and 40 characters.");
        }

        return trimmed;
    }

    private static string NormalizeUserNameLookup(string userName) => userName.ToUpperInvariant();

    private static LocalSession CreateSession(string userId, string userName)
    {
        var createdAt = DateTimeOffset.UtcNow;
        return new LocalSession(
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            userId,
            userName,
            createdAt,
            createdAt.Add(SessionTtl));
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100_000,
            numBytesRequested: 32);
        return $"PBKDF2$100000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string encoded)
    {
        var parts = encoded.Split('$');
        if (parts.Length != 4 || !string.Equals(parts[0], "PBKDF2", StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch
        {
            return false;
        }

        var actual = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: iterations,
            numBytesRequested: expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
