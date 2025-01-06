using System.Collections.Generic;
using System.Linq;
using ZeepkistClient;

namespace KoC.utils;

public static class ZeepkistNetworkExtensions
{
    public static int CountFavoritesInLobby(this IDictionary<uint, ZeepkistNetworkPlayer> players,
        IEnumerable<ulong> favorites)
    {
        return players.Count(player => favorites.Contains(player.Value.SteamID));
    }

    public static void SendCustomLocalMessage(string message)
    {
        if (ZeepkistNetwork.NetworkClient == null)
        {
            return;
        }

        ZeepkistNetwork.SendCustomChatMessage(false, ZeepkistNetwork.LocalPlayer.SteamID, message, "");
    }
}