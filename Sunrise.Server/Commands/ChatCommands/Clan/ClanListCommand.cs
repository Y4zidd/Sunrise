using Microsoft.Extensions.DependencyInjection;
using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;

namespace Sunrise.Server.Commands.ChatCommands.Clan;

[ChatCommand("list", "clan", isGlobal: false)]
public class ClanListCommand : IChatCommand
{
    public async Task Handle(Session session, ChatChannel? channel, string[]? args)
    {
        int? page = null;
        if (args != null && args.Length > 0)
        {
            if (args.Length != 1 || !int.TryParse(args[0], out var p))
            {
                ChatCommandRepository.SendMessage(session, "Invalid syntax: !clan list (page)");
                return;
            }
            page = p;
        }

        using var scope = ServicesProviderHolder.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Sunrise.Shared.Database.Repositories.ClanRepository>();

        var allCount = await repo.CountClans();
        if (allCount == 0)
        {
            ChatCommandRepository.SendMessage(session, "No clans found.");
            return;
        }

        var pageSize = 25;
        var clans = await repo.GetClans(page ?? 0, pageSize);
        var lines = new List<string> { $"sunrise clans listing ({allCount} total)." };
        var offset = (page ?? 0) * pageSize;
        foreach (var (clan, idx) in clans.Select((c, i) => (c, i)))
        {
            lines.Add($"{offset + idx + 1}. [{clan.Tag}] {clan.Name}");
        }

        ChatCommandRepository.SendMessage(session, string.Join('\n', lines));
    }
}


