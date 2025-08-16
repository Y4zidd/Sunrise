using Microsoft.Extensions.DependencyInjection;
using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Services;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;

namespace Sunrise.Server.Commands.ChatCommands.Clan;

[ChatCommand("disband", "clan", isGlobal: false)]
public class ClanDisbandCommand : IChatCommand
{
    public async Task Handle(Session session, ChatChannel? channel, string[]? args)
    {
        if (args != null && args.Length > 0)
        {
            ChatCommandRepository.SendMessage(session, "Usage: !clan disband");
            return;
        }

        using var scope = ServicesProviderHolder.CreateScope();
        var clanService = scope.ServiceProvider.GetRequiredService<ClanService>();
        var clanRepo = scope.ServiceProvider.GetRequiredService<Sunrise.Shared.Database.Repositories.ClanRepository>();

        var owned = await clanRepo.GetByOwner(session.UserId);
        if (owned == null)
        {
            ChatCommandRepository.SendMessage(session, "You must be the clan owner to disband the clan.");
            return;
        }

        var result = await clanService.Disband(session.UserId);
        if (result.IsFailure)
        {
            ChatCommandRepository.SendMessage(session, result.Error);
            return;
        }

        ChatCommandRepository.SendMessage(session, $"[{owned.Tag}] {owned.Name} disbanded.");
    }
}


