namespace KNOTS.Models;

public class FriendRequest
{
    public int Id { get; set; }
    public string RequesterUsername { get; set; } = string.Empty;
    public string ReceiverUsername { get; set; } = string.Empty;
    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public string PairKey { get; set; } = string.Empty;
}
