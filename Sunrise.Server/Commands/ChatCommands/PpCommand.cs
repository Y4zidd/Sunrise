using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Database;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;
using GameMode = Sunrise.Shared.Enums.Beatmaps.GameMode;

namespace Sunrise.Server.Commands.ChatCommands;

[ChatCommand("pp")]
public class PpCommand : IChatCommand
{
	public async Task Handle(Session session, ChatChannel? channel, string[]? args)
	{
		using var scope = ServicesProviderHolder.CreateScope();
		var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();

		var user = await database.Users.GetUser(id: session.UserId);
		if (user == null)
		{
			ChatCommandRepository.SendMessage(session, "User not found.");
			return;
		}

		// Optional clan tag fetch
		string? clanTag = null;
		if (user.ClanId > 0)
		{
			try
			{
				var clanRepo = scope.ServiceProvider.GetService<Sunrise.Shared.Database.Repositories.ClanRepository>();
				if (clanRepo != null)
				{
					var clan = await clanRepo.GetById(user.ClanId);
					clanTag = clan?.Tag;
				}
			}
			catch
			{
				// ignore clan lookup errors; proceed without tag
			}
		}

		var mode = (GameMode)session.Attributes.Status.PlayMode;
		var stats = await database.Users.Stats.GetUserStats(user.Id, mode);
		if (stats == null)
		{
			ChatCommandRepository.SendMessage(session, "No stats for this mode.");
			return;
		}

		// Format helpers
		string FormatPp(double pp)
		{
			if (pp >= 1_000_000) return (pp / 1_000_000d).ToString("0.##") + "m"; // e.g. 1.23m
			if (pp >= 1_000) return (pp / 1_000d).ToString("0.#") + "k";        // e.g. 71.3k
			return pp.ToString("0");
		}

		var ppFormatted = FormatPp(stats.PerformancePoints);
		var accFormatted = stats.Accuracy.ToString("0.00");

		var (globalRank, countryRank) = await database.Users.Stats.Ranks.GetUserRanks(user, mode);
		var rankPart = globalRank > 0 ? $" | Rank #{globalRank}" : string.Empty;
		var countryPart = countryRank > 0 ? $" (Country #{countryRank})" : string.Empty;

		// Indikasi kalau in-game panel ke-cap (short.MaxValue), server tetap punya angka full.
		var isCappedInPanel = stats.PerformancePoints > short.MaxValue;

		var nameWithClan = clanTag is not null ? $"[{clanTag}] {user.Username}" : user.Username;
		ChatCommandRepository.SendMessage(session,
			$"{nameWithClan}: {ppFormatted}pp{(isCappedInPanel ? " (full)" : string.Empty)} | Acc {accFormatted}%{rankPart}{countryPart}");
	}
}

