using System;
using ZeepSDK.ChatCommands;

namespace KoC.commands;

public class CommandVotingResult : ILocalChatCommand
{
    public static Action OnHandle;
    public string Prefix => "/";
    public string Command => "koc result";
    public string Description => "Ends the Votingphase and otherwise will not react";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }
}