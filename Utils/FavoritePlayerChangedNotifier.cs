using System;
using HarmonyLib;
using ZeepkistClient;

namespace KoC.Utils;

[HarmonyPatch(typeof(ZeepkistNetwork), nameof(ZeepkistNetwork.OnFavoritePlayer))]
public class FavoritePlayerChangedNotifier
{
    public static Action FavoritePlayersChanged;

    private static void Postfix()
    {
        FavoritePlayersChanged?.Invoke();
    }
}