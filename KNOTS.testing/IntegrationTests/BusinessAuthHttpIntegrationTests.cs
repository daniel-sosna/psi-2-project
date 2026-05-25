using System.Net;
using System.Net.Http.Json;
using KNOTS.Data;
using KNOTS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace KNOTS.testing.IntegrationTests;

public class BusinessAuthHttpIntegrationTests : IClassFixture<IntegrationTestApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestApplicationFactory _factory;

    public BusinessAuthHttpIntegrationTests(IntegrationTestApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task BusinessRegistrationAndLogin_OverHttp_CompletesFlow()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var businessName = $"Coffee Club {suffix}";
        var email = $"hello-{suffix}@coffee.test";
        var registerRequest = new BusinessRegisterRequest(businessName, email, "testpass");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/business/register", registerRequest);

        Assert.True(
            registerResponse.IsSuccessStatusCode,
            $"Register failed with {(int)registerResponse.StatusCode}: {await registerResponse.Content.ReadAsStringAsync()}");

        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<BusinessAuthResponse>();
        Assert.NotNull(registerPayload);
        Assert.True(registerPayload!.Success);
        Assert.Equal(businessName, registerPayload.Username);
        Assert.Equal(email, registerPayload.Email);
        Assert.True(registerPayload.IsBusiness);

        var loginRequest = new BusinessLoginRequest(email, "testpass");
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/business/login", loginRequest);

        Assert.True(
            loginResponse.IsSuccessStatusCode,
            $"Login failed with {(int)loginResponse.StatusCode}: {await loginResponse.Content.ReadAsStringAsync()}");

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<BusinessAuthResponse>();
        Assert.NotNull(loginPayload);
        Assert.True(loginPayload!.Success);
        Assert.Equal(businessName, loginPayload.Username);
        Assert.Equal(email, loginPayload.Email);
        Assert.True(loginPayload.IsBusiness);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedUser = dbContext.Users.Single(u => u.Email == email);

        Assert.Equal(businessName, savedUser.Username);
        Assert.Equal(UserType.Business, savedUser.UserType);
        Assert.NotEqual("testpass", savedUser.PasswordHash);
    }

    [Fact]
    public async Task BusinessLogin_OverHttp_WithWrongPassword_ReturnsUnauthorized()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var businessName = $"Bakery House {suffix}";
        var email = $"hello-{suffix}@bakery.test";

        await _client.PostAsJsonAsync(
            "/api/auth/business/register",
            new BusinessRegisterRequest(businessName, email, "testpass"));

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/business/login",
            new BusinessLoginRequest(email, "wrongpass"));

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task BusinessRegistration_OverHttp_WithDuplicateEmail_ReturnsBadRequest()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var firstBusinessName = $"Coffee Club {suffix}";
        var secondBusinessName = $"Bakery House {suffix}";
        var email = $"shared-{suffix}@coffee.test";

        var firstResponse = await _client.PostAsJsonAsync(
            "/api/auth/business/register",
            new BusinessRegisterRequest(firstBusinessName, email, "testpass"));

        var secondResponse = await _client.PostAsJsonAsync(
            "/api/auth/business/register",
            new BusinessRegisterRequest(secondBusinessName, email, "testpass"));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var payload = await secondResponse.Content.ReadFromJsonAsync<BusinessAuthResponse>();
        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Contains("email", payload.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BusinessRegistration_OverHttp_WithInvalidEmail_ReturnsBadRequest()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await _client.PostAsJsonAsync(
            "/api/auth/business/register",
            new BusinessRegisterRequest($"Coffee Club {suffix}", $"invalid-email-{suffix}", "testpass"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<BusinessAuthResponse>();
        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Contains("valid email", payload.Message, StringComparison.OrdinalIgnoreCase);
    }
}
