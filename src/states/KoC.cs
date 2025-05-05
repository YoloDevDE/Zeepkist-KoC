using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crosstales;
using KoC.commands;
using KoC.models;
using KoC.utils;
using ZeepkistClient;
using ZeepSDK.Messaging;
using ZeepSDK.Multiplayer;

namespace KoC.states;

public class KoC
{
    public SubmissionLevel CachedSubmissionLevel;
    public VotingLevel CurrentVotingLevel;
    public bool Enabled;
    public Dictionary<ulong, float> OriginalVoteTime = new Dictionary<ulong, float>();
    public BaseState State;
    public SubmissionLevel SubmissionLevel;
    public List<VotingLevel> VotingLevels;

    public KoC()
    {
        VotingLevels = Plugin.Instance.GetVotingLevels();
        CommandStart.OnHandle += Enable;
        CommandStop.OnHandle += Disable;
        State = new StateDisabled(this);
        State.Enter();
    }

    public List<ZeepkistNetworkPlayer> EligibleVoters { get; set; }
    public bool OverrideSubmission { get; set; }

    public void InitializeEligibleVoters()
    {
        EligibleVoters = new List<ZeepkistNetworkPlayer>();
        EligibleVoters.AddRange(ZeepkistNetwork.Players.Values);
    }

    public void Enable()
    {
        if (!Enabled)
        {
            MultiplayerApi.DisconnectedFromGame += Disable;
            MultiplayerApi.PlayerJoined += MultiplayerApiOnPlayerJoined;
            TransitionTo(new StateCheckCachedLevel(this));
            ChatUtils.SendCustomChatMessage(StartMessage());
        }
        else
        {
            Plugin.Instance.Messenger.LogWarning("Already started");
        }
    }

    private static async void MultiplayerApiOnPlayerJoined(ZeepkistNetworkPlayer player)
    {
        ZeepkistNetwork.TryGetPlayer(player.SteamID, out ZeepkistNetworkPlayer localPlayer);
        while (player.Zeepkist.displayRuntime <= 0)
        {
            await Task.Yield(); // Wait for a frame update instead of a time delay
        }

        string ctToHexRGB = player.chatColor.CTToHexRGB();
        string message = $"Welcome <b><color=#{ctToHexRGB}>{player.GetTaggedUsername()}</color></b> to <color=#ff8800>Kick</color><color=#4444ff> or</color><color=#ff8800> Clutch</color>!<br>" +
                         "Here we play and vote on maps which get uploaded to " +
                         "<color=#FF0000>youtube.com/@owlplague</color><br>" +
                         "When on the voting map, vote by finishing in either the <color=#ff0000>KICK</color> or <color=#00ff00>CLUTCH</color> finish.<br>" +
                         "<color=#ff0000>Don't bother using the mapper finish - your time won't count!</color><br>" +
                         "Want to submit your own maps? Join the CTR Discord server to participate!";
        ChatUtils.SendCustomChatMessage(message, player.SteamID);
    }

    private string StartMessage()
    {
        string restrictedVoting =
            (Plugin.Instance.OnlyEligiblePlayersCanVote.Value ? "<#00FF00>ON" : "<#FF0000>OFF") +
            "</color>";
        string msg = "<color=#c2c2c2><color=#ff8800>Kick</color><color=#4444ff> or</color><color=#ff8800> Clutch</color> started!<br>" +
                     $"Restricted Voting: {restrictedVoting}</color>";
        // "Welcome to <color=#ff8800>Kick</color><color=#4444ff> or</color><color=#ff8800> Clutch</color>!<br>" +
        // "Here we play and vote on maps which get uploaded to " +
        // "<color=#FF0000>youtube.com/@owlplague</color><br>" +
        // "When on the voting map, vote by finishing in either the <color=#ff8800>KICK</color> or <color=#4444ff>CLUTCH</color> finish.<br>" +
        // "<color=#ff0000>Don't bother using the mapper finish - your time won't count!</color><br>" +
        // "Want to submit your own maps? Join the CTR Discord server to participate!";
        return msg;
    }

    public void Disable()
    {
        if (Enabled)
        {
            MessengerApi.Log("KoC stopped");
            MultiplayerApi.DisconnectedFromGame -= Disable;
            MultiplayerApi.PlayerJoined -= MultiplayerApiOnPlayerJoined;
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

    public bool RemoveFromLeaderBoardIfNotNeutral(ZeepkistNetworkPlayer item)
    {
        if (IsLocalPlayer(item.SteamID) ||
            IsFavorite(item.SteamID) ||
            IsAuthor(item.SteamID))
        {
            return false;
        }

        ZeepkistNetworkPlayer player =
            ZeepkistNetwork.PlayerList.FirstOrDefault(x => x.SteamID == item.SteamID);
        if (player != null)
        {
            ZeepkistNetwork.CustomLeaderBoard_RemovePlayerFromLeaderboard(player.SteamID, false);
            ZeepkistNetwork.SendCustomChatMessage(false, player.SteamID, "You are not the mapper - Please vote according to your opinion on the previous map", "KoC");
        }

        return true;
    }

    public void TransitionTo(BaseState state)
    {
        State.Exit();
        State = state;
        State.Enter();
    }
}