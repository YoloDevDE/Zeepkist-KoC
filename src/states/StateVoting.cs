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
        ChatApi.SendMessage("/joinmessage orange " + Plugin.Instance.JoinMessageVoting);
        ChatApi.SendMessage(Plugin.Instance.AutoMessage);
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
    }

    private void OnPlayerJoined(ZeepkistNetworkPlayer player)
    {
        if (!koC.IsEligibleForVoting(player.SteamID) && Plugin.Instance.OnlyEligiblePlayersCanVote.Value)
        {
            ZeepkistNetwork.CustomLeaderBoard_BlockPlayerFromSettingTime(player.SteamID, false);
            ZeepkistNetwork.SendCustomChatMessage(false, player.SteamID, "You are not eligible to vote because you haven't played on the previous map", "KoC");
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
        // Assemble the final message
        string resultServerMessage =
            ChatUtils.UpdateVotingResultsMessage(votesClutch: koC.SubmissionLevel.VotesClutch, votesKick: koC.SubmissionLevel.VotesKick, submissionLevel: koC.SubmissionLevel);;
        if (KoC.SubmissionLevel.VotesClutch < KoC.SubmissionLevel.VotesKick)
        {
            ChatApi.SendMessage("<br>--KICK--<br>" +
                                $"Sorry to {KoC.SubmissionLevel.Author} :/<br>" +
                                $"You flopped with {KoC.SubmissionLevel.VotesClutch} to {KoC.SubmissionLevel.VotesKick} votes..<br>" +
                                "You will now get kicked o7");
            resultServerMessage += "<#ff0000>KICK";
        }
        else
        {
            ChatApi.SendMessage("<br>--ClUTCH--<br>" +
                                $"Congratulations to {KoC.SubmissionLevel.Author} :party:<br>" +
                                $"You clutched with {KoC.SubmissionLevel.VotesClutch} to {KoC.SubmissionLevel.VotesKick} votes!<br>" +
                                "Enjoy your free win!");

            resultServerMessage += "<#00ff00>CLUTCH";
        }


        ChatApi.SendMessage(resultServerMessage);

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

        if (player.CurrentResult.Time >= 35000f || player.CurrentResult.Time <= 0.0000001f)
        {
            return;
        }


        if (player.CurrentResult.Time < CurrentVotingLevel.ClutchFinishTime)
        {
            if (KoC.RemoveFromLeaderBoardIfNotNeutral(player))
            {
                return;
            }

            ZeepkistNetwork.CustomLeaderBoard_SetPlayerLeaderboardOverrides(player.SteamID, "<color=#bbbb00>MAPPER</color>", position: " ");
            ZeepkistNetwork.CustomLeaderBoard_SetPlayerTimeOnLeaderboard(player.SteamID, 0.0000001f, false);
            koC.SubmissionLevel.RemoveVote(player.SteamID);
        }

        // Skip ineligible voters if only eligible players can vote
        else if (!KoC.IsEligibleForVoting(player.SteamID) && Plugin.Instance.OnlyEligiblePlayersCanVote.Value)
        {
            ZeepkistNetwork.SendCustomChatMessage(false, player.SteamID, "You are not eligible to vote this time, because you haven't played the previous map", "KoC");
            return;
        }

        // Count votes for kick or clutch based on the player's time
        else if (player.CurrentResult.Time >= CurrentVotingLevel.KickFinishTime)
        {
            KoC.SubmissionLevel.AddVoteKick(player.SteamID);
            ZeepkistNetwork.CustomLeaderBoard_SetPlayerLeaderboardOverrides(player.SteamID, "<color=#bb0000>KICK</color>", position: " ");
            ZeepkistNetwork.CustomLeaderBoard_SetPlayerTimeOnLeaderboard(player.SteamID, 36000f, false);
        }
        else
        {
            KoC.SubmissionLevel.AddVoteClutch(player.SteamID);
            ZeepkistNetwork.CustomLeaderBoard_SetPlayerLeaderboardOverrides(player.SteamID, "<color=#00bb00>CLUTCH</color>", position: " ");
            ZeepkistNetwork.CustomLeaderBoard_SetPlayerTimeOnLeaderboard(player.SteamID, 35000f, false);
        }

        ChatApi.SendMessage(ChatUtils.UpdateVotingResultsMessage(votesClutch: koC.SubmissionLevel.VotesClutch, votesKick: koC.SubmissionLevel.VotesKick, submissionLevel: koC.SubmissionLevel));
    }

    // In Utils class


// Updated Method
}