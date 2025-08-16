namespace Sunrise.Shared.Database.Objects.Clan;

public class ClanLeaderboardItem
{
    public int ClanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public int MemberCount { get; set; }
    public double Value { get; set; }
    public double AvgAcc { get; set; }
    public long PlayCount { get; set; }
    public int Rank { get; set; }
}

