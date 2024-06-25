using System;
using ZeepSDK.ChatCommands;

namespace KoC.Commands;

public class CommandZivecef : ILocalChatCommand
{
    public static Action OnHandle;
    public string Prefix => "/";
    public string Command => "zivecef";
    public string Description => "MEGAKIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIICK!!!";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }
}