namespace DndApp.Api.Auth;

public sealed record LocalRegisterRequest(string UserName, string Password);
public sealed record LocalLoginRequest(string UserName, string Password);

public sealed record LocalSession(
    string SessionToken,
    string UserId,
    string UserName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);
