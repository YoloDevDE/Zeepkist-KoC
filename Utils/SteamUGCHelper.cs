using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;

namespace KoC.Utils;

public class SteamUGCHelper
{
    public async Task<ulong> GetSteamIdFromWorkshopItemAsync(PublishedFileId publishedFileId)
    {
        Item? item = await SteamUGC.QueryFileAsync(publishedFileId);
        if (item.HasValue)
        {
            return item.Value.Owner.Id.Value;
        }

        return 0;
    }
}