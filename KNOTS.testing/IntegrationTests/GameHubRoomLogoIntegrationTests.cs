using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace KNOTS.testing.IntegrationTests;

public class GameHubRoomLogoIntegrationTests : IClassFixture<IntegrationTestApplicationFactory>, IAsyncLifetime
{
    private readonly IntegrationTestApplicationFactory _factory;
    private HubConnection? _hostConnection;
    private HubConnection? _guestConnection;

    public GameHubRoomLogoIntegrationTests(IntegrationTestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateRoom_ThenJoinRoom_PropagatesBusinessLogo()
    {
        _hostConnection = BuildHubConnection();
        _guestConnection = BuildHubConnection();

        var roomCreated = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var joinedRoom = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

        _hostConnection.On<string>("RoomCreated", roomCode => roomCreated.TrySetResult(roomCode));
        _guestConnection.On<object>("JoinedRoom", roomData =>
        {
            var json = JsonSerializer.Serialize(roomData);
            joinedRoom.TrySetResult(JsonSerializer.Deserialize<JsonElement>(json));
        });

        await _hostConnection.StartAsync();
        await _guestConnection.StartAsync();

        await _hostConnection.InvokeAsync("JoinGame", "Coffee Club");
        await _guestConnection.InvokeAsync("JoinGame", "Alice");

        var logoUrl = "/uploads/business-logos/integration-logo.png";
        await _hostConnection.InvokeAsync("CreateRoom", "Coffee Club", logoUrl);

        var roomCode = await roomCreated.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await _guestConnection.InvokeAsync("JoinRoom", roomCode, "Alice");
        var joinedPayload = await joinedRoom.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(roomCode, GetStringProperty(joinedPayload, "RoomCode"));
        Assert.Equal(logoUrl, GetStringProperty(joinedPayload, "BusinessLogoDataUrl"));

        var players = GetArrayProperty(joinedPayload, "Players").Select(player => player.GetString()).ToList();
        Assert.Contains("Coffee Club", players);
        Assert.Contains("Alice", players);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_hostConnection != null)
        {
            await _hostConnection.DisposeAsync();
        }

        if (_guestConnection != null)
        {
            await _guestConnection.DisposeAsync();
        }
    }

    private HubConnection BuildHubConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/gamehub"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property)
            ? property.GetString()
            : null;
    }

    private static IEnumerable<JsonElement> GetArrayProperty(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property)
            ? property.EnumerateArray()
            : Enumerable.Empty<JsonElement>();
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.TryGetProperty(propertyName, out property))
        {
            return true;
        }

        var camelCaseName = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        return element.TryGetProperty(camelCaseName, out property);
    }
}
