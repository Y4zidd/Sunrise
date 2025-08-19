using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Database;
using Sunrise.Shared.Enums.Users;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;

namespace Sunrise.Server.Commands.ChatCommands.Development;

[ChatCommand("listbadges", requiredPrivileges: UserPrivilege.Developer)]
public class ListCustomBadgesCommand : IChatCommand
{
    public async Task Handle(Session session, ChatChannel? channel, string[]? args)
    {
        if (args == null || args.Length < 1 || !int.TryParse(args[0], out var userId))
        {
            ChatCommandRepository.SendMessage(session, $"Usage: {Configuration.BotPrefix}listbadges <user id>");
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

        // built-in badges (berdasarkan privilege)
        var builtin = new List<string>();
        if (user.Privilege.HasFlag(UserPrivilege.Developer)) builtin.Add("Developer");
        if (user.Privilege.HasFlag(UserPrivilege.Admin)) builtin.Add("Admin");
        if (user.Privilege.HasFlag(UserPrivilege.Bat)) builtin.Add("BAT");
        if (user.IsUserSunriseBot()) builtin.Add("Bot");
        if (user.Privilege.HasFlag(UserPrivilege.Supporter)) builtin.Add("Supporter");

        var custom = await database.Users.CustomBadges.GetBadges(userId);

        var builtinList = builtin.Count == 0 ? "(none)" : string.Join(", ", builtin);
        var customList = custom.Count == 0 ? "(none)" : string.Join(", ", custom);

        ChatCommandRepository.SendMessage(session, $"Builtin badges of {user.Username} ({user.Id}): {builtinList}");
        ChatCommandRepository.SendMessage(session, $"Custom badges of {user.Username} ({user.Id}): {customList}");
    }
}


