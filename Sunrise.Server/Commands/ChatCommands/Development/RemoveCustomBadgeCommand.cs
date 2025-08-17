using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Database;
using Sunrise.Shared.Enums.Users;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;

namespace Sunrise.Server.Commands.ChatCommands.Development;

[ChatCommand("delbadge", requiredPrivileges: UserPrivilege.Developer)]
public class RemoveCustomBadgeCommand : IChatCommand
{
    public async Task Handle(Session session, ChatChannel? channel, string[]? args)
    {
        if (args == null || args.Length < 2)
        {
            ChatCommandRepository.SendMessage(session, $"Usage: {Configuration.BotPrefix}delbadge <user id> <badge1> [badge2] [badge3] ...");
            return;
        }

        if (!int.TryParse(args[0], out var userId))
        {
            ChatCommandRepository.SendMessage(session, "Invalid user id.");
            return;
        }

        var badges = args[1..].Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (badges.Length == 0)
        {
            ChatCommandRepository.SendMessage(session, "No badges provided.");
            return;
        }

        using var scope = ServicesProviderHolder.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();

        var user = await database.Users.GetUser(userId);
        if (user == null)
        {
            ChatCommandRepository.SendMessage(session, "User not found.");
            return;
        }

        var existing = await database.Users.CustomBadges.GetBadges(userId);
        var toRemove = badges.Where(b => existing.Contains(b, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (toRemove.Length == 0)
        {
            ChatCommandRepository.SendMessage(session, $"No matching custom badges found on {user.Username} ({user.Id}).");
            return;
        }

        var result = await database.Users.CustomBadges.RemoveBadges(userId, toRemove);
        if (result.IsFailure)
        {
            ChatCommandRepository.SendMessage(session, result.Error);
            return;
        }

        ChatCommandRepository.SendMessage(session, $"Removed badges from {user.Username} ({user.Id}): {string.Join(", ", toRemove)}");
    }
}


