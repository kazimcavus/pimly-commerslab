namespace Identity.Api.Requests;

/// <summary>POST /api/v1/identity/login istek gövdesi.</summary>
public sealed record LoginRequest(string Email, string Password);
