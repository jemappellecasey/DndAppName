using System.Collections.Concurrent;

namespace DndApp.Api.Auth;

public interface ILocalAuthService
{
    LocalSession Login(LocalLoginRequest request);
    LocalSession? GetSession(string sessionToken);
}

public sealed class LocalAuthService : ILocalAuthService
{
    private readonly ConcurrentDictionary<string, LocalSession> _sessions = new(StringComparer.Ordinal);

    public LocalSession Login(LocalLoginRequest request)
    {
        var userName = string.IsNullOrWhiteSpace(request.UserName) ? "player" : request.UserName.Trim();
        var userId = $"local:{userName.ToLowerInvariant()}";
        var session = new LocalSession(Guid.NewGuid().ToString("N"), userId, userName, DateTimeOffset.UtcNow);
        _sessions[session.SessionToken] = session;
        return session;
    }

    public LocalSession? GetSession(string sessionToken)
    {
        return _sessions.TryGetValue(sessionToken, out var session) ? session : null;
    }
}
