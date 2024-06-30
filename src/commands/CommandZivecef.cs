using System;
using ZeepSDK.Chat;
using ZeepSDK.ChatCommands;

namespace KoC.commands;

public class CommandZivecef : ILocalChatCommand
{
    public static Action OnHandle;
    public string Prefix => "/";
    public string Command => "zivecef";
    public string Description => "MEGAKIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIICK!!!";

    public void Handle(string arguments)
    {
        // OnHandle?.Invoke();
        ChatApi.AddLocalMessage("Megakick is not implemented yet :(");
    }
}