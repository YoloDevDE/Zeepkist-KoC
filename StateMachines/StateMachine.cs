using System.Collections.Generic;
using System.Linq;
using KoC.Commands;
using KoC.Data;
using KoC.States;
using ZeepSDK.Chat;
using ZeepSDK.Messaging;
using ZeepSDK.Multiplayer;

namespace KoC;

public class StateMachine
{
    public SubmissionLevel CurrentSubmissionLevel;
    public VotingLevel CurrentVotingLevel;
    public bool Enabled;
    public BaseState State;
    public List<VotingLevel> VotingLevels;

    public StateMachine()
    {
        VotingLevels = Plugin.Instance.GetVotingLevels();
        CommandStart.OnHandle += Enable;
        CommandStop.OnHandle += Disable;
        State = new StateDisabled(this);
        State.Enter();
    }

    public void Enable()
    {
        if (!Enabled)
        {
            MultiplayerApi.DisconnectedFromGame += Disable;
            TransitionTo(new StateRegisterSubmission(this));
            ChatApi.SendMessage("KoC started");
        }
        else
        {
            Plugin.Instance.Messenger.LogWarning("Already started");
        }
    }

    public void Disable()
    {
        if (Enabled)
        {
            MessengerApi.Log("KoC stopped");
            MultiplayerApi.DisconnectedFromGame -= Disable;
            TransitionTo(new StateDisabled(this));
        }
        else
        {
            Plugin.Instance.Messenger.LogWarning("Already stopped");
        }
    }

    public VotingLevel GetVotingLevelByUid(string uid)
    {
        return VotingLevels.FirstOrDefault(level => level.LevelUid == uid);
    }

    public void TransitionTo(BaseState state)
    {
        State.Exit();
        State = state;
        State.Enter();
    }
}