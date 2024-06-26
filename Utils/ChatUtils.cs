using System;
using ZeepkistClient;

namespace KoC.Utils;

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
}