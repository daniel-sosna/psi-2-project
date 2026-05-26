using System;
using Bunit;
using KNOTS.Components.Pages;
using NSubstitute;

public class LoginPage_BusinessValidationTests : LoginPageTestsBase
{
    [Fact]
    public void BusinessSignup_WithInvalidEmail_ShowsValidationMessage_AndDoesNotCallService()
    {
        _userServiceMock.IsAuthenticated.Returns(false);

        var cut = Render<Login>();

        cut.FindAll("button.account-type-btn")[1].Click();
        cut.Find("#businessNameInput").Change("Coffee Club");
        cut.Find("#businessEmailInput").Change("not-an-email");
        cut.Find("#passwordInput").Change("secret");

        cut.Find("button.btn-signup").Click();

        _userServiceMock.DidNotReceive().RegisterBusinessUser(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        Assert.Contains("Please enter a valid email address.", cut.Markup);
    }

    [Fact]
    public void BusinessLogin_WithEmptyEmail_ShowsValidationMessage_AndDoesNotCallService()
    {
        _userServiceMock.IsAuthenticated.Returns(false);

        var cut = Render<Login>();

        cut.FindAll("button.account-type-btn")[1].Click();
        cut.Find("#businessNameInput").Change("Coffee Club");
        cut.Find("#businessEmailInput").Change(string.Empty);
        cut.Find("#passwordInput").Change("secret");

        cut.Find("button.btn-login").Click();

        _userServiceMock.DidNotReceive().LoginBusinessUser(Arg.Any<string>(), Arg.Any<string>());
        Assert.Contains("Please enter a valid email address.", cut.Markup);
    }
}
