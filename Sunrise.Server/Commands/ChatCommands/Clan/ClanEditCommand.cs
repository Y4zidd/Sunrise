using Microsoft.Extensions.DependencyInjection;
using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Services;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;

namespace Sunrise.Server.Commands.ChatCommands.Clan;

[ChatCommand("edit", "clan", isGlobal: false)]
public class ClanEditCommand : IChatCommand
{
    public async Task Handle(Session session, ChatChannel? channel, string[]? args)
    {
        if (args == null || args.Length < 2)
        {
            ChatCommandRepository.SendMessage(session, "Usage: !clan edit <name|tag> <new value>");
            return;
        }

        var editType = args[0].ToLowerInvariant();
        var newValue = string.Join(' ', args.Skip(1));

        if (editType != "name" && editType != "tag")
        {
            ChatCommandRepository.SendMessage(session, "Invalid edit type. Use 'name' or 'tag'.");
            return;
        }

        using var scope = ServicesProviderHolder.CreateScope();
        var clanService = scope.ServiceProvider.GetRequiredService<ClanService>();
        var clanRepo = scope.ServiceProvider.GetRequiredService<Sunrise.Shared.Database.Repositories.ClanRepository>();

        var owned = await clanRepo.GetByOwner(session.UserId);
        if (owned == null)
        {
            ChatCommandRepository.SendMessage(session, "You must be the clan owner to edit clan details.");
            return;
        }

        var oldName = owned.Name;
        var oldTag = owned.Tag;
        
        string? newName = editType == "name" ? newValue : null;
        string? newTag = editType == "tag" ? newValue : null;

        var result = await clanService.EditClan(session.UserId, newName, newTag);
        if (result.IsFailure)
        {
            ChatCommandRepository.SendMessage(session, result.Error);
            return;
        }

        var editedField = editType == "name" ? "name" : "tag";
        var oldValue = editType == "name" ? oldName : oldTag;
        
        ChatCommandRepository.SendMessage(session, 
            $"Clan {editedField} changed from '{oldValue}' to '{newValue}'. " +
            $"Clan is now: [{result.Value.Tag}] {result.Value.Name}");
    }
}
