using Microsoft.Extensions.DependencyInjection;
using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Services;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;

namespace Sunrise.Server.Commands.ChatCommands.Clan;

[ChatCommand("join", "clan", isGlobal: false)]
public class ClanJoinCommand : IChatCommand
{
    public async Task Handle(Session session, ChatChannel? channel, string[]? args)
    {
        if (args == null || args.Length < 1)
        {
            ChatCommandRepository.SendMessage(session, "Usage: !clan join <clanId>");
            return;
        }

        if (!int.TryParse(args[0], out var clanId))
        {
            ChatCommandRepository.SendMessage(session, "Invalid clan id.");
            return;
        }

        using var scope = ServicesProviderHolder.CreateScope();
        var clanService = scope.ServiceProvider.GetRequiredService<ClanService>();

        var result = await clanService.JoinClan(session.UserId, clanId);
        if (result.IsFailure)
        {
            ChatCommandRepository.SendMessage(session, result.Error);
            return;
        }

        ChatCommandRepository.SendMessage(session, $"Joined clan {clanId}.");
    }
}


