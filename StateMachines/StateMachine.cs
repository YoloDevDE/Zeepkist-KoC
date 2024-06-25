using System.Collections.Generic;
using KoC.Commands;
using KoC.Data;
using KoC.States;
using ZeepSDK.Messaging;
using ZeepSDK.Multiplayer;

namespace KoC;

public class StateMachine
{
    public bool Enabled;
    public Plugin Plugin;
    public BaseState State;
    public SubmissionLevel SubmissionLevel;
    public List<VotingLevel> VotingLevels;

    public StateMachine(Plugin plugin)
    {
        Plugin = plugin;
        VotingLevels = Plugin.GetVotingLevels();
        CommandStart.OnHandle += Enable;
        CommandStop.OnHandle += Disable;
        State = new StateDisabled(this);
        State.Enter();
    }

    public void Enable()
    {
        if (!Enabled)
        {
            MessengerApi.Log("KoC started");
            MultiplayerApi.DisconnectedFromGame += Disable;
            SwitchState(new StateRegisterSubmission(this));
        }
        else
        {
            Plugin.Messenger.LogWarning("Already started");
        }
    }

    public void Disable()
    {
        if (Enabled)
        {
            MessengerApi.Log("KoC stopped");
            MultiplayerApi.DisconnectedFromGame -= Disable;
            State.Exit();
            State = new StateDisabled(this);
            State.Enter();
        }
        else
        {
            Plugin.Messenger.LogWarning("Already stopped");
        }
    }

    public void SwitchState(BaseState state)
    {
        State.Exit();
        State = state;
        State.Enter();
    }
}