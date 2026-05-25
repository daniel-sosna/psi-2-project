namespace KNOTS.Models;

public record BusinessRegisterRequest(string BusinessName, string Email, string Password);

public record BusinessLoginRequest(string Email, string Password);

public record BusinessAuthResponse(bool Success, string Message, string? Username, string? Email, bool IsBusiness);
