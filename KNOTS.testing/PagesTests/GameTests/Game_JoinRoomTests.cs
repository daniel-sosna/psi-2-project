using Bunit;
using KNOTS.Components.Pages;

public class Game_JoinRoomTests : GameTestBase
{
    [Fact]
    public void ClickingJoinRoom_InvokesJSAndShowsStatus()
    {
        // Arrange
        var (component, _, _, _, _) = SetupGameComponent(currentUser: "Bob");
        JSInterop.SetupVoid("joinRoom", _ => true);

        // Act
        component.Find(".room-code-input").Change("1234");
        component.Find(".btn-join").Click();

        // Assert
        Assert.Contains("Connecting to room 1234", component.Markup);
    }
}
