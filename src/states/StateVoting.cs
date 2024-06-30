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

public class StateVoting(StateMachine stateMachine) : BaseState(stateMachine)
{
    private VotingLevel CurrentVotingLevel { get; set; }

    public override void Enter()
    {
        // Initialize the current voting level
        CurrentVotingLevel = StateMachine.GetVotingLevelByUid(ZeepkistNetwork.CurrentLobby.LevelUID);
        StateMachine.CurrentVotingLevel = CurrentVotingLevel;
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
        StateMachine.TransitionTo(new StatePostVoting(StateMachine));
    }

    private void OnVotingFinished()
    {
        if (StateMachine.SubmissionLevel.VotesClutch < StateMachine.SubmissionLevel.VotesKick)
        {
            ChatApi.SendMessage("<br>--KICK--<br>" +
                                $"Sorry to {StateMachine.SubmissionLevel.Author} :/<br>" +
                                $"You flopped with {StateMachine.SubmissionLevel.VotesClutch} to {StateMachine.SubmissionLevel.VotesKick} votes..<br>" +
                                "You will now get kicked o7");
            ChatApi.SendMessage(ParseMessage("/servermessage red 0 " + Plugin.Instance.ResultServerMessage));
        }
        else
        {
            ChatApi.SendMessage("<br>--ClUTCH--<br>" +
                                $"Congratulations to {StateMachine.SubmissionLevel.Author} :party:<br>" +
                                $"You clutched with {StateMachine.SubmissionLevel.VotesClutch} to {StateMachine.SubmissionLevel.VotesKick} votes!<br>" +
                                "Enjoy your freewin!");
            ChatApi.SendMessage(ParseMessage("/servermessage green 0 " + Plugin.Instance.ResultServerMessage));
        }

        StateMachine.TransitionTo(new StatePostVoting(StateMachine));
    }

    private void OnPlayerResultsChanged(ZeepkistNetworkPlayer player)
    {
        ProcessVotes();
    }

    private void ProcessVotes()
    {
        StateMachine.SubmissionLevel.ResetVotes();

        foreach (LeaderboardItem leaderboardItem in ZeepkistNetwork.Leaderboard)
        {
            // Kick players with a time below the ClutchFinishTime
            if (IsUsingMapperFinish(leaderboardItem))
            {
                StateMachine.KickIfNotNeutralPlayer(leaderboardItem);
                continue;
            }

            // Skip eligible voters if only eligible players can vote
            if (StateMachine.IsEligibleForVoting(leaderboardItem.SteamID) && Plugin.Instance.OnlyEligiblePlayersCanVote.Value)
            {
                // Count votes for kick or clutch based on the player's time
                if (leaderboardItem.Time >= CurrentVotingLevel.KickFinishTime)
                {
                    StateMachine.SubmissionLevel.VotesKick++;
                }
                else
                {
                    StateMachine.SubmissionLevel.VotesClutch++;
                }
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
                            $"{StateMachine.SubmissionLevel.Name} by {StateMachine.SubmissionLevel.Author}<br>" +
                            $"Kick: {StateMachine.SubmissionLevel.VotesKick} | Clutch: {StateMachine.SubmissionLevel.VotesClutch}")
            ;
    }


    private string ParseMessage(string message)
    {
        return message
                .Replace("%a", StateMachine.SubmissionLevel.Author)
                .Replace("%l", StateMachine.SubmissionLevel.Name)
                .Replace("%r", VotingResultString())
                .Replace("%c", StateMachine.SubmissionLevel.VotesClutch.ToString())
                .Replace("%k", StateMachine.SubmissionLevel.VotesKick.ToString())
            ;
    }

    private string VotingResultString()
    {
        return StateMachine.SubmissionLevel.VotesClutch >= StateMachine.SubmissionLevel.VotesKick ? "Clutch" : "Kick";
    }
}