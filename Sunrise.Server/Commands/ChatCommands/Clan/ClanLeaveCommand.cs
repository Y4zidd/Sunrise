using Microsoft.Extensions.DependencyInjection;
using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Services;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;
using Sunrise.Shared.Database;

namespace Sunrise.Server.Commands.ChatCommands.Clan;

[ChatCommand("leave", "clan", isGlobal: false)]
public class ClanLeaveCommand : IChatCommand
{
    public async Task Handle(Session session, ChatChannel? channel, string[]? args)
    {
        if (args != null && args.Length > 0)
        {
            ChatCommandRepository.SendMessage(session, "Usage: !clan leave");
            return;
        }

        using var scope = ServicesProviderHolder.CreateScope();
        var clanService = scope.ServiceProvider.GetRequiredService<ClanService>();
        var clanRepo = scope.ServiceProvider.GetRequiredService<Sunrise.Shared.Database.Repositories.ClanRepository>();

        var owned = await clanRepo.GetByOwner(session.UserId);

        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        var user = await database.Users.GetUser(session.UserId);
        var currentClan = user != null && user.ClanId != 0 ? await clanRepo.GetById(user.ClanId) : null;

        var result = await clanService.LeaveClan(session.UserId);
        if (result.IsFailure)
        {
            var suffix = owned != null
                ? " You must transfer your clan's ownership before leaving it. Alternatively, you can use !clan disband."
                : string.Empty;
            ChatCommandRepository.SendMessage(session, result.Error + suffix);
            return;
        }

        var display = currentClan != null ? $"[{currentClan.Tag}] {currentClan.Name}" : "your clan";
        ChatCommandRepository.SendMessage(session, $"You have successfully left {display}.");
    }
}


