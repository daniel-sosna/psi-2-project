namespace KNOTS.Models;

public class FriendSearchResult
{
    public string Username { get; set; } = string.Empty;
    public bool CanSendRequest { get; set; }
    public bool IsFriend { get; set; }
    public bool HasPendingIncomingRequest { get; set; }
    public bool HasPendingOutgoingRequest { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
}
