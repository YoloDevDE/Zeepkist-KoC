using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using ZeepSDK.Chat;

namespace KoC.Utils;

public class SteamUGCHelper
{
    public async Task<ulong> GetSteamIdFromWorkshopItemAsync(PublishedFileId publishedFileId)
    {
        // Asynchrone Abfrage des Workshop-Items
        Item? item = await SteamUGC.QueryFileAsync(publishedFileId);

        // Überprüfen, ob das Ergebnis vorhanden ist und gültig ist
        if (item.HasValue)
        {
            return item.Value.Owner.Id.Value;
        }

        // Rückgabe von 0, falls das Item nicht gefunden wurde oder ungültig ist
        return 0;
    }
}