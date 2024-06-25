using System.Collections.Generic;
using System.Linq;
using ZeepkistClient;

namespace KoC.Utils;

public static class ZeepkistNetworkExtensions
{
    public static int CountFavoritesInLobby(this IDictionary<uint, ZeepkistNetworkPlayer> players, IEnumerable<ulong> favorites)
    {
        return players.Count(player => favorites.Contains(player.Value.SteamID));
    }
}