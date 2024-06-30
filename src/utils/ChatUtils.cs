using System;
using System.Linq;
using System.Threading.Tasks;
using ZeepkistClient;

namespace KoC.utils;

public class ChatUtils
{
    public static void AddNewChatMessage(ZeepkistChatMessage message)
    {
        ZeepkistNetwork.ChatMessages.Add(message);
        if (ZeepkistNetwork.ChatMessages.Count > 20)
        {
            ZeepkistNetwork.ChatMessages.RemoveAt(0);
        }

        Action<ZeepkistChatMessage> chatMessageReceived = ZeepkistNetwork.ChatMessageReceived;
        if (chatMessageReceived != null)
        {
            chatMessageReceived(message);
        }
    }

    public static async void RemoveJoinMessage()
    {
        if (ZeepkistNetwork.ChatMessages.Any(msg => msg.Message.Contains("[host] Join message set and enabled!")))
        {
            ZeepkistNetwork.ChatMessages.RemoveAll(msg => msg.Message.Contains("[host] Join message set and enabled!"));
            PlayerManager.Instance.currentMaster.OnlineGameplayUI.ChatUI.OnClose();
            PlayerManager.Instance.currentMaster.OnlineGameplayUI.ChatUI.OnOpen();
            await RefreshChatAsync();
        }
    }

    private static async Task RefreshChatAsync()
    {
        await Task.Delay(500);
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.ChatUI.OnClose();
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.ChatUI.OnOpen();
    }
}