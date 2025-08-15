using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Sunrise.Shared.Application;
using Sunrise.Shared.Services;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;

namespace Sunrise.Server.Commands.ChatCommands.Clan;

[ChatCommand("create", "clan", isGlobal: false)]
public class ClanCreateCommand : IChatCommand
{
    public async Task Handle(Session session, ChatChannel? channel, string[]? args)
    {
        if (args == null || args.Length < 2)
        {
            ChatCommandRepository.SendMessage(session, "Usage: !clan create <tag> <name>");
            return;
        }

        var tag = args[0];
        var name = string.Join(' ', args.Skip(1));

        using var scope = ServicesProviderHolder.CreateScope();
        var clanService = scope.ServiceProvider.GetRequiredService<ClanService>();

        var result = await clanService.CreateClan(session.UserId, name, tag, null);
        if (result.IsFailure)
        {
            ChatCommandRepository.SendMessage(session, result.Error);
            return;
        }

        ChatCommandRepository.SendMessage(session, $"Clan created: [{result.Value.Tag}] {result.Value.Name}");
    }
}
