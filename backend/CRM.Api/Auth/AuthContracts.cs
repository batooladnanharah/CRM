namespace CRM.Api.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(AuthUserDto User, string Token);

public sealed record AuthUserDto(
    Guid Id, string Name, string Email, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

public sealed record ErrorResponse(string Message);
