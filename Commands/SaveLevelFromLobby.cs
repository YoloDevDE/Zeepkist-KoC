using System;
using ZeepSDK.ChatCommands;

namespace KoC.Commands;

public class SaveLevelFromLobby : ILocalChatCommand
{
    public static Action OnHandle;
    public string Prefix => "/";
    public string Command => "koc save";
    public string Description => "";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }
}