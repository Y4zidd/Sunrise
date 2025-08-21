using Microsoft.Extensions.DependencyInjection;
using Sunrise.Shared.Application;
using Sunrise.Shared.Database;
using Sunrise.Shared.Repositories;
using Sunrise.Shared.Utils.Tools;

namespace Sunrise.API.Services;

public class AssetService(DatabaseService database)
{
    public async Task<(bool, string?)> SetBanner(int userId, Stream fileStream)
    {
        var (isValid, err) = ImageTools.IsHasValidImageAttributes(fileStream);
        if (!isValid || err != null)
            return (false, err);
        
        var addOrUpdateBannerResult = await database.Users.Files.AddOrUpdateBanner(userId, fileStream);

        if (addOrUpdateBannerResult.IsFailure)
            return (false, "Failed to save banner. Please try again later.");

        return (true, null);
    }

    public async Task<(bool, string?)> SetAvatar(int userId, Stream fileStream)
    {
        var (isValid, err) = ImageTools.IsHasValidImageAttributes(fileStream);
        if (!isValid || err != null)
            return (false, err);

        var addOrUpdateAvatarResult = await database.Users.Files.AddOrUpdateAvatar(userId, fileStream);

        if (addOrUpdateAvatarResult.IsFailure)
            return (false, "Failed to save avatar. Please try again later.");

        return (true, null);
    }

    public async Task<(bool, string?)> SetClanAvatar(int clanId, Stream fileStream)
    {
        var (isValid, err) = ImageTools.IsHasValidImageAttributes(fileStream);
        if (!isValid || err != null)
            return (false, err);

        try
        {
            // Store as GIF if input is GIF, otherwise PNG; remove existing files regardless of extension
            var dirPath = Path.Combine(Configuration.DataPath, "Files/Clan/Avatars");
            Directory.CreateDirectory(dirPath);
            foreach (var existing in Directory.EnumerateFiles(dirPath, $"{clanId}.*", SearchOption.TopDirectoryOnly))
            {
                try { File.Delete(existing); } catch { }
            }
            var inputType = ImageTools.GetImageType(fileStream);
            var outputExt = string.Equals(inputType, "gif", StringComparison.OrdinalIgnoreCase) ? "gif" : "png";
            var filePath = Path.Combine(dirPath, $"{clanId}.{outputExt}");
            var resized = ImageTools.ResizeImage(fileStream, 256, 256);
            var ok = await LocalStorageRepository.WriteFileAsync(filePath, resized);
            if (!ok) return (false, "Failed to save clan avatar. Please try again later.");
            return (true, null);
        }
        catch
        {
            return (false, "Failed to save clan avatar. Please try again later.");
        }
    }

    public async Task<(bool, string?)> SetClanBanner(int clanId, Stream fileStream)
    {
        var (isValid, err) = ImageTools.IsHasValidImageAttributes(fileStream);
        if (!isValid || err != null)
            return (false, err);

        try
        {
            // Store as GIF if input is GIF, otherwise PNG; remove existing files regardless of extension
            var dirPath = Path.Combine(Configuration.DataPath, "Files/Clan/Banners");
            Directory.CreateDirectory(dirPath);
            foreach (var existing in Directory.EnumerateFiles(dirPath, $"{clanId}.*", SearchOption.TopDirectoryOnly))
            {
                try { File.Delete(existing); } catch { }
            }
            var inputType = ImageTools.GetImageType(fileStream);
            var outputExt = string.Equals(inputType, "gif", StringComparison.OrdinalIgnoreCase) ? "gif" : "png";
            var filePath = Path.Combine(dirPath, $"{clanId}.{outputExt}");
            var resized = ImageTools.ResizeImage(fileStream, 1280, 320);
            var ok = await LocalStorageRepository.WriteFileAsync(filePath, resized);
            if (!ok) return (false, "Failed to save clan banner. Please try again later.");
            return (true, null);
        }
        catch
        {
            return (false, "Failed to save clan banner. Please try again later.");
        }
    }
}