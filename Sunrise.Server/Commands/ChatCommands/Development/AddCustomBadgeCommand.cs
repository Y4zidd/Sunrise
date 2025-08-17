using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Database;
using Sunrise.Shared.Enums.Users;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;

namespace Sunrise.Server.Commands.ChatCommands.Development;

[ChatCommand("addbadge", requiredPrivileges: UserPrivilege.Developer)]
public class AddCustomBadgeCommand : IChatCommand
{
    private static bool IsHex(string token)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(token, "^([0-9a-fA-F]{6}|[0-9a-fA-F]{3})$");
    }

    public async Task Handle(Session session, ChatChannel? channel, string[]? args)
    {
        if (args == null || args.Length < 2)
        {
            ChatCommandRepository.SendMessage(session, $"Usage: {Configuration.BotPrefix}addbadge <user id> <name> [#RRGGBB] [icon]");
            return;
        }

        if (!int.TryParse(args[0], out var userId))
        {
            ChatCommandRepository.SendMessage(session, "Invalid user id.");
            return;
        }

        // Syntax: !addbadge <userId> <badge-name> [#RRGGBB] [icon]
        var parts = args[1..].Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (parts.Length == 0)
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

        var name = parts[0];
        string? color = null;
        string? icon = null;

        var idx = 1;
        if (idx < parts.Length)
        {
            // Accept formats: "#b4befe", "b4befe", "# b4befe"
            if (parts[idx] == "#" && idx + 1 < parts.Length && IsHex(parts[idx + 1]))
            {
                color = "#" + parts[idx + 1];
                idx += 2;
            }
            else if ((parts[idx].StartsWith('#') && parts[idx].Length > 1 && IsHex(parts[idx][1..])) || IsHex(parts[idx]))
            {
                color = parts[idx].StartsWith('#') ? parts[idx] : "#" + parts[idx];
                idx += 1;
            }
        }

        if (idx < parts.Length)
        {
            if (parts[idx] == "#" && idx + 1 < parts.Length) idx += 1; // skip stray '#'
            if (idx < parts.Length) icon = parts[idx];
        }

        var addResult = await database.Users.CustomBadges.AddBadges(userId, new[] { name });
        if (addResult.IsFailure)
        {
            ChatCommandRepository.SendMessage(session, addResult.Error);
            return;
        }

        if (!string.IsNullOrWhiteSpace(color))
        {
            var colorResult = await database.Users.CustomBadges.SetBadgeColor(userId, name, color!);
            if (colorResult.IsFailure)
            {
                ChatCommandRepository.SendMessage(session, $"Added badge '{name}', but color invalid: {colorResult.Error}");
            }
        }

        if (!string.IsNullOrWhiteSpace(icon))
        {
            var iconValue = icon!; // FE akan menerjemahkan string ini (lucide/emoji/url) tanpa prefix
            var iconResult = await database.Users.CustomBadges.SetBadgeIcon(userId, name, iconValue, null);
            if (iconResult.IsFailure)
            {
                ChatCommandRepository.SendMessage(session, $"Added badge '{name}', but icon invalid: {iconResult.Error}");
            }
        }

        var summary = name;
        if (color != null) summary += $" (color={color})";
        if (icon != null) summary += $" (icon={icon})";
        ChatCommandRepository.SendMessage(session, $"Added badge to {user.Username} ({user.Id}): {summary}");
    }
}


