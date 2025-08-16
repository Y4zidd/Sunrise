using Microsoft.Extensions.DependencyInjection;
using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Services;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;

namespace Sunrise.Server.Commands.ChatCommands.Clan;

[ChatCommand("demote", "clan", isGlobal: false)]
public class ClanDemoteCommand : IChatCommand
{
    public async Task Handle(Session session, ChatChannel? channel, string[]? args)
    {
        if (args == null || args.Length < 1 || !int.TryParse(args[0], out var targetUserId))
        {
            ChatCommandRepository.SendMessage(session, "Invalid syntax: !clan demote <userId>");
            return;
        }

        using var scope = ServicesProviderHolder.CreateScope();
        var clanService = scope.ServiceProvider.GetRequiredService<ClanService>();
        var clanRepo = scope.ServiceProvider.GetRequiredService<Sunrise.Shared.Database.Repositories.ClanRepository>();

        var owned = await clanRepo.GetByOwner(session.UserId);
        if (owned == null)
        {
            ChatCommandRepository.SendMessage(session, "You must be the clan owner to demote members.");
            return;
        }

        var result = await clanService.DemoteToMember(session.UserId, targetUserId);
        if (result.IsFailure)
        {
            ChatCommandRepository.SendMessage(session, result.Error);
            return;
        }

        ChatCommandRepository.SendMessage(session, $"Demoted user id {targetUserId} to Member.");
    }
}


