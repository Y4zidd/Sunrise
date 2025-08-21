using System.Text.Json.Serialization;
using Sunrise.API.Enums;
using Sunrise.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Sunrise.Shared.Application;
using Sunrise.Shared.Database;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Shared.Enums.Beatmaps;
using Sunrise.Shared.Enums.Users;
using Sunrise.Shared.Extensions.Users;
using Sunrise.Shared.Repositories;
using Sunrise.Shared.Utils.Converters;

namespace Sunrise.API.Serializable.Response;

public class UserResponse
{

    [JsonConstructor]
    public UserResponse()
    {
    }

    public UserResponse(SessionRepository sessionRepository, User user)
    {
        var session = sessionRepository.GetSession(userId: user.Id);

        Id = user.Id;
        Username = user.Username;
        Description = user.Description;
        Country = user.Country;
        RegisterDate = user.RegisterDate;
        UserStatus = session != null ? session.Attributes.Status.ToText() : "Offline";
        AvatarUrl = user.AvatarUrl;
        BannerUrl = user.BannerUrl;
        ClanId = user.ClanId;
        ClanPriv = user.ClanPriv;
        LastOnlineTime = session != null ? session.Attributes.LastPingRequest : user.LastOnlineTime;
        IsRestricted = user.IsRestricted();
        SilencedUntil = user.SilencedUntil > DateTime.UtcNow ? user.SilencedUntil : null!;
        DefaultGameMode = user.DefaultGameMode;
        Badges = UserService.GetUserBadges(user);
        using var scope = ServicesProviderHolder.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        CustomBadges = db.Users.CustomBadges.GetBadges(user.Id).Result;
        CustomBadgesDetailed = db.Users.CustomBadges.GetBadgesDetailed(user.Id).Result
            .Select(b => new UserCustomBadgeResponse(b.Name, b.ColorHex, b.Icon, b.IconType)).ToList();
    }

    [JsonPropertyName("user_id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("country_code")]
    public CountryCode Country { get; set; }

    [JsonPropertyName("register_date")]
    [JsonConverter(typeof(DateTimeWithTimezoneConverter))]
    public DateTime RegisterDate { get; set; }

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; }

    [JsonPropertyName("banner_url")]
    public string BannerUrl { get; set; }


    [JsonPropertyName("last_online_time")]
    [JsonConverter(typeof(DateTimeWithTimezoneConverter))]
    public DateTime LastOnlineTime { get; set; }

    [JsonPropertyName("restricted")]
    public bool IsRestricted { get; set; }

    [JsonPropertyName("silenced_until")]
    [JsonConverter(typeof(DateTimeWithTimezoneConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? SilencedUntil { get; set; }

    [JsonPropertyName("default_gamemode")]
    public GameMode DefaultGameMode { get; set; }

    [JsonPropertyName("badges")]
    public List<UserBadge> Badges { get; set; }

    [JsonPropertyName("user_status")]
    public string UserStatus { get; set; }

    [JsonPropertyName("custom_badges")]
    public List<string> CustomBadges { get; set; } = new();

    [JsonPropertyName("custom_badges_detailed")]
    public List<UserCustomBadgeResponse> CustomBadgesDetailed { get; set; } = new();

    [JsonPropertyName("clan_id")]
    public int ClanId { get; set; }

    [JsonPropertyName("clan_priv")]
    public byte ClanPriv { get; set; }
}

public record UserCustomBadgeResponse(string Name, string? ColorHex, string? Icon, string? IconType);
