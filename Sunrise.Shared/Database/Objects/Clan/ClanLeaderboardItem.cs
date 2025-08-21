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
    public string AvatarUrl => $"https://a.{Shared.Application.Configuration.Domain}/clan/avatar/{ClanId}{BuildVersionSuffix("Files/Clan/Avatars", ClanId)}";
    public string BannerUrl => $"https://a.{Shared.Application.Configuration.Domain}/clan/banner/{ClanId}{BuildVersionSuffix("Files/Clan/Banners", ClanId)}";
    private static string BuildVersionSuffix(string folder, int clanId)
    {
        try
        {
            var dir = System.IO.Path.Combine(Shared.Application.Configuration.DataPath, folder);
            var existing = System.IO.Directory.Exists(dir)
                ? System.IO.Directory.EnumerateFiles(dir, $"{clanId}.*", System.IO.SearchOption.TopDirectoryOnly).FirstOrDefault()
                : null;
            if (existing != null)
            {
                var ts = new DateTimeOffset(System.IO.File.GetLastWriteTimeUtc(existing)).ToUnixTimeMilliseconds();
                return $"?{ts}";
            }
        }
        catch { }
        return string.Empty;
    }
}

