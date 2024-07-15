using System;
using KoC.commands;
using KoC.models;
using KoC.utils;
using ZeepkistClient;
using ZeepkistNetworking;
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

        // Initial calls
        ProcessVotes();

        // Send messages
        ChatApi.SendMessage("/joinmessage orange " + Plugin.Instance.JoinMessageVoting);
        ChatApi.SendMessage(Plugin.Instance.AutoMessage);
    }

    private void OnSettingChanged(object sender, EventArgs e)
    {
        ProcessVotes();
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
        ProcessVotes();
    }

    private void OnPlayerLeft(ZeepkistNetworkPlayer player)
    {
        ProcessVotes();
    }

    private void OnFavoritePlayersChanged()
    {
        ProcessVotes();
    }

    private void OnRoundEnded()
    {
        OnVotingFinished();
        KoC.TransitionTo(new StatePostVoting(KoC));
    }

    private void OnVotingFinished()
    {
        if (KoC.SubmissionLevel.VotesClutch < KoC.SubmissionLevel.VotesKick)
        {
            ChatApi.SendMessage("<br>--KICK--<br>" +
                                $"Sorry to {KoC.SubmissionLevel.Author} :/<br>" +
                                $"You flopped with {KoC.SubmissionLevel.VotesClutch} to {KoC.SubmissionLevel.VotesKick} votes..<br>" +
                                "You will now get kicked o7");
            ChatApi.SendMessage(ParseMessage("/servermessage red 0 " + Plugin.Instance.ResultServerMessage));
        }
        else
        {
            ChatApi.SendMessage("<br>--ClUTCH--<br>" +
                                $"Congratulations to {KoC.SubmissionLevel.Author} :party:<br>" +
                                $"You clutched with {KoC.SubmissionLevel.VotesClutch} to {KoC.SubmissionLevel.VotesKick} votes!<br>" +
                                "Enjoy your freewin!");
            ChatApi.SendMessage(ParseMessage("/servermessage green 0 " + Plugin.Instance.ResultServerMessage));
        }

        KoC.TransitionTo(new StatePostVoting(KoC));
    }

    private void OnPlayerResultsChanged(ZeepkistNetworkPlayer player)
    {
        ProcessVotes();
    }

    private void ProcessVotes()
    {
        KoC.SubmissionLevel.ResetVotes();

        foreach (LeaderboardItem leaderboardItem in ZeepkistNetwork.Leaderboard)
        {
            // Kick players with a time below the ClutchFinishTime
            if (IsUsingMapperFinish(leaderboardItem))
            {
                KoC.KickIfNotNeutralPlayer(leaderboardItem);
                continue;
            }

            // Skip eligible voters if only eligible players can vote
            if (!KoC.IsEligibleForVoting(leaderboardItem.SteamID) && Plugin.Instance.OnlyEligiblePlayersCanVote.Value)
            {
                continue;
            }

            // Count votes for kick or clutch based on the player's time
            if (leaderboardItem.Time >= CurrentVotingLevel.KickFinishTime)
            {
                KoC.SubmissionLevel.VotesKick++;
            }
            else
            {
                KoC.SubmissionLevel.VotesClutch++;
            }
        }

        UpdateVotingResultsMessage();
    }

    private bool IsUsingMapperFinish(LeaderboardItem leaderboardItem)
    {
        return leaderboardItem.Time < CurrentVotingLevel.ClutchFinishTime;
    }


    private void UpdateVotingResultsMessage()
    {
        ChatApi.SendMessage($"/servermessage yellow 0 " +
                            $"{KoC.SubmissionLevel.Name} by {KoC.SubmissionLevel.Author}<br>" +
                            $"Kick: {KoC.SubmissionLevel.VotesKick} | Clutch: {KoC.SubmissionLevel.VotesClutch}")
            ;
    }


    private string ParseMessage(string message)
    {
        return message
                .Replace("%a", KoC.SubmissionLevel.Author)
                .Replace("%l", KoC.SubmissionLevel.Name)
                .Replace("%r", VotingResultString())
                .Replace("%c", KoC.SubmissionLevel.VotesClutch.ToString())
                .Replace("%k", KoC.SubmissionLevel.VotesKick.ToString())
            ;
    }

    private string VotingResultString()
    {
        return KoC.SubmissionLevel.VotesClutch >= KoC.SubmissionLevel.VotesKick ? "Clutch" : "Kick";
    }
}