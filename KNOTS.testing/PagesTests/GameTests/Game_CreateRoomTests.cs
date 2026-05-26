using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using KNOTS.Components.Pages;
using System.Text.Json;

public class Game_CreateRoomTests : GameTestBase
{
    [Fact]
    public void ClickingCreateRoom_InvokesJSAndShowsStatus()
    {
        // Arrange
        var (component, _, _, _, _) = SetupGameComponent();
        JSInterop
            .Setup<JsonElement>("createRoom", _ => true)
            .SetResult(JsonSerializer.Deserialize<JsonElement>("{\"success\":true,\"errorMessage\":\"\"}"));

        // Act
        var button = component.Find(".btn-create");
        button.Click();

        // Assert
        component.WaitForAssertion(() =>
            Assert.Contains("Creating room with topics", component.Markup));
    }
}
