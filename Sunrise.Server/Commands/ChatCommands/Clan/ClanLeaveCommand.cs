using Microsoft.Extensions.DependencyInjection;
using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Services;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;

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

        var result = await clanService.LeaveClan(session.UserId);
        if (result.IsFailure)
        {
            ChatCommandRepository.SendMessage(session, result.Error);
            return;
        }

        ChatCommandRepository.SendMessage(session, "Left clan.");
    }
}


