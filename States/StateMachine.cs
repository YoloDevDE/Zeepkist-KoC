using System.Collections.Generic;
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
    public VotingLevel CurrentVotingLevel;
    public bool Enabled;
    public BaseState State;
    public SubmissionLevel SubmissionLevel;
    public List<VotingLevel> VotingLevels;


    public StateMachine()
    {
        VotingLevels = Plugin.Instance.GetVotingLevels();
        CommandStart.OnHandle += Enable;
        CommandStop.OnHandle += Disable;
        State = new StateDisabled(this);
        State.Enter();
    }

    public List<ZeepkistNetworkPlayer> EligibleVoters { get; set; }
    public bool OverrideSubmission { get; set; }


    public void Enable()
    {
        if (!Enabled)
        {
            EligibleVoters = new List<ZeepkistNetworkPlayer>();
            ZeepkistNetwork.ChatMessageReceived += OnChatMessageReceived;
            MultiplayerApi.DisconnectedFromGame += Disable;
            TransitionTo(new StateRegisterSubmission(this));
            ChatUtils.AddNewChatMessage(StartMessage());
        }
        else
        {
            Plugin.Instance.Messenger.LogWarning("Already started");
        }
    }

    private void OnChatMessageReceived(ZeepkistChatMessage msg)
    {
        ChatUtils.RemoveJoinMessage();
    }

    private ZeepkistChatMessage StartMessage()
    {
        string restrictedVoting = (Plugin.Instance.OnlyEligiblePlayersCanVote.Value ? "<#00FF00>ON" : "<#FF0000>OFF") + "</color>";
        ZeepkistChatMessage msg = new ZeepkistChatMessage();
        msg.Message = "<i><#00AA00>Kick or Clutch started!<br>" +
                      $"Restricted Voting:</color> {restrictedVoting}</i>";
        return msg;
    }

    public void Disable()
    {
        if (Enabled)
        {
            MessengerApi.Log("KoC stopped");
            MultiplayerApi.DisconnectedFromGame -= Disable;

            ZeepkistNetwork.ChatMessageReceived -= OnChatMessageReceived;
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

    public bool IsLocalPlayer(ulong steamID)
    {
        return steamID == ZeepkistNetwork.LocalPlayer.SteamID;
    }

    public bool IsAuthor(ulong steamID)
    {
        return steamID == SubmissionLevel.AuthorSteamId;
    }

    public bool IsFavorite(ulong steamID)
    {
        return ZeepkistNetwork.CurrentLobby.Favorites.Contains(steamID);
    }

    public bool IsEligibleForVoting(ulong steamID)
    {
        return EligibleVoters.Any(voter => voter.SteamID == steamID);
    }

    public void KickIfNotNeutralPlayer(LeaderboardItem item)
    {
        if (IsLocalPlayer(item.SteamID) || IsFavorite(item.SteamID) || IsAuthor(item.SteamID))
        {
            return;
        }

        ZeepkistNetworkPlayer player = ZeepkistNetwork.PlayerList.FirstOrDefault(x => x.SteamID == item.SteamID);
        if (player != null)
        {
            ZeepkistNetwork.KickPlayer(player);
        }
    }

    public void TransitionTo(BaseState state)
    {
        State.Exit();
        State = state;
        State.Enter();
    }
}