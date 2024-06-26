﻿using System.Collections.Generic;
using System.Linq;
using KoC.Commands;
using KoC.Data;
using KoC.Utils;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Messaging;
using ZeepSDK.Multiplayer;

namespace KoC.States;

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
            ChatUtils.AddNewChatMessage(StartMessage());
        }
        else
        {
            Plugin.Instance.Messenger.LogWarning("Already started");
        }
    }

    private ZeepkistChatMessage StartMessage()
    {
        ZeepkistChatMessage msg = new ZeepkistChatMessage();
        msg.Message = "<#00AA00><i>Kick or Clutch started!</i></color>";
        return msg;
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

    public bool IsNeutral(ulong steamID)
    {
        return steamID == ZeepkistNetwork.LocalPlayer.SteamID || steamID == CurrentSubmissionLevel.AuthorSteamId || ZeepkistNetwork.CurrentLobby.Favorites.Contains(steamID);
    }

    public void KickNonNeutralPlayer(LeaderboardItem item)
    {
        if (!IsNeutral(item.SteamID))
        {
            ZeepkistNetworkPlayer player = ZeepkistNetwork.PlayerList.FirstOrDefault(x => x.SteamID == item.SteamID);
            if (player != null)
            {
                ZeepkistNetwork.KickPlayer(player);
            }
        }
    }

    public void TransitionTo(BaseState state)
    {
        State.Exit();
        State = state;
        State.Enter();
    }
}