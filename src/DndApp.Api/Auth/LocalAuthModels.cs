namespace DndApp.Api.Auth;

public sealed record LocalLoginRequest(string UserName);

public sealed record LocalSession(
    string SessionToken,
    string UserId,
    string UserName,
    DateTimeOffset CreatedAtUtc);
