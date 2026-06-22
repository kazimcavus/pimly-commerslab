namespace Identity.Application.Users.Login;

/// <summary>Kullanıcı giriş komutu.</summary>
public sealed record LoginCommand(string Email, string Password);
