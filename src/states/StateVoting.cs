using System;
using KoC.commands;
using KoC.models;
using KoC.utils;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace KoC.states;

public class StateVoting(KoC koC) : BaseState(koC)
{
    private int _finishCounter;
    private VotingLevel CurrentVotingLevel { get; set; }

    public override void Enter()
    {
        // Initialize the current voting level
        CurrentVotingLevel = KoC.GetVotingLevelByUid(ZeepkistNetwork.CurrentLobby.LevelUID);
        KoC.CurrentVotingLevel = CurrentVotingLevel;
        // Subscribe to events
        ZeepkistNetwork.PlayerResultsChanged += OnPlayerResultsChanged;
        MultiplayerApi.PlayerJoined += OnPlayerJoined;
        MultiplayerApi.PlayerLeft += OnPlayerLeft;
        RacingApi.RoundEnded += OnRoundEnded;
        RacingApi.PlayerSpawned += OnPlayerSpawned;
        Plugin.Instance.OnlyEligiblePlayersCanVote.SettingChanged += OnSettingChanged;
        FavoritePlayerChangedNotifier.FavoritePlayersChanged += OnFavoritePlayersChanged;
        CommandVotingResult.OnHandle += OnVotingFinished;

        foreach (ZeepkistNetworkPlayer player in ZeepkistNetwork.PlayerList)
        {
            if (!koC.IsEligibleForVoting(player.SteamID) && Plugin.Instance.OnlyEligiblePlayersCanVote.Value)
            {
                ZeepkistNetwork.CustomLeaderBoard_BlockPlayerFromSettingTime(player.SteamID, false);
                ZeepkistNetwork.SendCustomChatMessage(false, player.SteamID, "You are not eligible to vote because you haven't played on the previous map", "KoC");
            }
        }

        // Initial calls
        ProcessVotes(null);

        // Send messages
        ChatUtils.SendCustomChatMessage(Plugin.Instance.AutoMessage);
    }

    private void OnPlayerSpawned()
    {
        if (PlayerManager.Instance.currentMaster.isPhotoMode)
        {
            return;
        }

        PlayerManager.Instance.currentMaster.flyingCamera.ToggleFlyingCamera();
        SpectatorCameraUI flyingCameraSpectatorCameraUI = PlayerManager.Instance.currentMaster.flyingCamera.SpectatorCameraUI;
        flyingCameraSpectatorCameraUI.FOV.enabled = false;
        flyingCameraSpectatorCameraUI.inputDisplay.enabled = false;
        flyingCameraSpectatorCameraUI.SmallTooltips.enabled = false;
        flyingCameraSpectatorCameraUI.Target.enabled = false;
        flyingCameraSpectatorCameraUI.Target_SteamID.enabled = false;
        flyingCameraSpectatorCameraUI.Mode.enabled = false;
        RacingApi.PlayerSpawned -= OnPlayerSpawned;
    }

    private void OnSettingChanged(object sender, EventArgs e)
    {
        ProcessVotes(null);
    }

    public override void Exit()
    {
        // Unsubscribe from events
        ZeepkistNetwork.PlayerResultsChanged -= OnPlayerResultsChanged;
        MultiplayerApi.PlayerJoined -= OnPlayerJoined;
        MultiplayerApi.PlayerLeft -= OnPlayerLeft;
        RacingApi.RoundEnded -= OnRoundEnded;
        Plugin.Instance.OnlyEligiblePlayersCanVote.SettingChanged -= OnSettingChanged;
        FavoritePlayerChangedNotifier.FavoritePlayersChanged -= OnFavoritePlayersChanged;
        CommandVotingResult.OnHandle -= OnVotingFinished;
        RacingApi.PlayerSpawned -= OnPlayerSpawned;
    }

    private void OnPlayerJoined(ZeepkistNetworkPlayer player)
    {
        if (!koC.IsEligibleForVoting(player.SteamID) && Plugin.Instance.OnlyEligiblePlayersCanVote.Value)
        {
            ZeepkistNetwork.CustomLeaderBoard_BlockPlayerFromSettingTime(player.SteamID, false);
            ChatUtils.SendCustomChatMessage("You are not eligible to vote because you haven't played on the previous map", player.SteamID);
            return;
        }

        ProcessVotes(player);
    }

    private void OnPlayerLeft(ZeepkistNetworkPlayer player)
    {
        ProcessVotes(player);
    }

    private void OnFavoritePlayersChanged()
    {
        ProcessVotes(null);
    }

    private void OnRoundEnded()
    {
        OnVotingFinished();
        KoC.TransitionTo(new StatePostVoting(KoC));
    }

    private void OnVotingFinished()
    {
        bool isKick = KoC.SubmissionLevel.VotesClutch < KoC.SubmissionLevel.VotesKick;
        string resultColor = isKick ? "ff0000" : "00ff00";
        string resultTextClutch = "<#00ff00>C</color><#00ee00>L</color><#00dd00>U</color><#00cc00>T</color><#00bb00>C</color><#00aa00>H</color>";
        string resultTextKick = "<#ff0000>K</color><#ff1100>I</color><#ff2200>C</color><#ff3300>K</color>";

        string baseText = isKick ? resultTextKick : resultTextClutch;
        int baseTextLength = isKick ? 4 : 6; // Raw length without color codes
        int paddingLength = (31 - baseTextLength) / 2 + 4;
        string padding = new string(' ', paddingLength - baseTextLength - 3);
        string resultText = $"{padding}{(isKick ? ":skull: " : "  :sparkle: ")}{baseText}{(isKick ? " :skull:" : " :sparkle:")}";

        string messageText = isKick
            ? $"<i>Sorry</i> <#ffa500><b>{KoC.SubmissionLevel.Author}</b></color> :/<br>" +
              $"You <b><u>flopped</u></b> with <#00ff00><b>{KoC.SubmissionLevel.VotesClutch}</b></color> to <#ff0000><b>{KoC.SubmissionLevel.VotesKick}</b></color> votes..<br>" +
              "<i>You will now get kicked</i> o7"
            : $"<i>Congratulations</i> <#ffa500><b>{KoC.SubmissionLevel.Author}</b></color> :party:<br>" +
              $"You <b><u>clutched</u></b> with <#00ff00><b>{KoC.SubmissionLevel.VotesClutch}</b></color> to <#ff0000><b>{KoC.SubmissionLevel.VotesKick}</b></color> votes!<br>" +
              "<i>Enjoy your free win!</i>";

        string resultServerMessage = ChatUtils.UpdateVotingResultsMessage(
            votesClutch: KoC.SubmissionLevel.VotesClutch,
            votesKick: KoC.SubmissionLevel.VotesKick,
            submissionLevel: KoC.SubmissionLevel);

        ChatUtils.SendCustomChatMessage($"{messageText}", $"<b>{resultText.Trim()}</b>");
        ChatApi.SendMessage($"{resultServerMessage}{resultText}");

        KoC.TransitionTo(new StatePostVoting(KoC));
    }

    private void OnPlayerResultsChanged(ZeepkistNetworkPlayer player)
    {
        ProcessVotes(player);
    }

    private void ProcessVotes(ZeepkistNetworkPlayer player)
    {
        if (player == null)
        {
            ChatApi.SendMessage(ChatUtils.UpdateVotingResultsMessage(votesClutch: koC.SubmissionLevel.VotesClutch, votesKick: koC.SubmissionLevel.VotesKick, submissionLevel: koC.SubmissionLevel));
            return;
        }

        if (player.CurrentResult == null)
        {
            return;
        }

        if (player.CurrentResult.Time >= 600f - _finishCounter || player.CurrentResult.Time <= 0.0000001f)
        {
            return;
        }


        if (player.CurrentResult.Time < CurrentVotingLevel.ClutchFinishTime)
        {
            if (KoC.RemoveFromLeaderBoardIfNotNeutral(player))
            {
                return;
            }

            ZeepkistNetwork.CustomLeaderBoard_SetPlayerLeaderboardOverrides(player.SteamID, "<color=#bbbb00>MAPPER</color>", position: "<color=#ff0000><b><sprite=\"Zeepkist\" name=\"YannicSmile\"></b></color>");
            ZeepkistNetwork.CustomLeaderBoard_SetPlayerTimeOnLeaderboard(player.SteamID, 0.0000001f, false);
            koC.SubmissionLevel.RemoveVote(player.SteamID);
            koC.OriginalVoteTime.Remove(player.SteamID);
        }

        // Skip ineligible voters if only eligible players can vote
        else if (!KoC.IsEligibleForVoting(player.SteamID) && Plugin.Instance.OnlyEligiblePlayersCanVote.Value)
        {
            ChatUtils.SendCustomChatMessage("You are not eligible to vote this time, because you haven't played the previous map", player.SteamID);
            return;
        }

        // Count votes for kick or clutch based on the player's time
        else if (player.CurrentResult.Time >= CurrentVotingLevel.KickFinishTime)
        {
            koC.OriginalVoteTime[player.SteamID] = player.CurrentResult.Time;
            KoC.SubmissionLevel.AddVoteKick(player.SteamID);
            ZeepkistNetwork.CustomLeaderBoard_SetPlayerLeaderboardOverrides(player.SteamID, "<color=#bb0000>KICK</color>", position: "<color=#ff0000><b><sprite=\"Zeepkist\" name=\"Skull\"></b></color>");
            ZeepkistNetwork.CustomLeaderBoard_SetPlayerTimeOnLeaderboard(player.SteamID, 600f - _finishCounter++, false);
        }
        else
        {
            koC.OriginalVoteTime[player.SteamID] = player.CurrentResult.Time;
            KoC.SubmissionLevel.AddVoteClutch(player.SteamID);
            ZeepkistNetwork.CustomLeaderBoard_SetPlayerLeaderboardOverrides(player.SteamID, "<color=#00bb00>CLUTCH</color>", position: "<color=#ff0000><b><sprite=\"Zeepkist\" name=\"Sparkle\"></b></color>");
            ZeepkistNetwork.CustomLeaderBoard_SetPlayerTimeOnLeaderboard(player.SteamID, 600f - _finishCounter++, false);
        }

        ChatApi.SendMessage(ChatUtils.UpdateVotingResultsMessage(votesClutch: koC.SubmissionLevel.VotesClutch, votesKick: koC.SubmissionLevel.VotesKick, submissionLevel: koC.SubmissionLevel));
    }

    // In Utils class


// Updated Method
}