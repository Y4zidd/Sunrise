using Microsoft.Extensions.DependencyInjection;
using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;

namespace Sunrise.Server.Commands.ChatCommands.Clan;

[ChatCommand("info", "clan", isGlobal: false)]
public class ClanInfoCommand : IChatCommand
{
    public async Task Handle(Session session, ChatChannel? channel, string[]? args)
    {
        if (args == null || args.Length < 1)
        {
            ChatCommandRepository.SendMessage(session, "Invalid syntax: !clan info <tag>");
            return;
        }

        using var scope = ServicesProviderHolder.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Sunrise.Shared.Database.Repositories.ClanRepository>();
        var usersRepo = scope.ServiceProvider.GetRequiredService<Sunrise.Shared.Database.Repositories.UserRepository>();

        var tag = string.Join(' ', args).ToUpperInvariant();
        var clan = await repo.GetByTag(tag);
        if (clan == null)
        {
            ChatCommandRepository.SendMessage(session, "Could not find a clan by that tag.");
            return;
        }

        var members = await usersRepo.GetUsersByClanId(clan.Id);
        var lines = new List<string> { $"[{clan.Tag}] {clan.Name} | Founded {clan.CreatedAt:MMM dd, yyyy}." };
        foreach (var m in members.OrderByDescending(m => m.Id == clan.OwnerId ? 3 : m.ClanPriv))
        {
            var privStr = m.Id == clan.OwnerId
                ? "Owner"
                : (m.ClanPriv == 2 ? "Officer" : "Member");
            lines.Add($"[{privStr}] {m.Username}");
        }

        ChatCommandRepository.SendMessage(session, string.Join('\n', lines));
    }
}


