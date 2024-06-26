using System;
using KoC.Commands;
using KoC.Data;
using KoC.Utils;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace KoC.States;

public class StateVoting : BaseState
{
    public StateVoting(StateMachine stateMachine) : base(stateMachine)
    {
    }

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
        FavoritePlayerChangedNotifier.FavoritePlayersChanged += OnFavoritePlayersChanged;
        CommandVotingResult.OnHandle += OnVotingFinished;

        // Initial calls
        UpdateVotes();

        // Send messages
        ChatApi.SendMessage("/joinmessage orange " + Plugin.Instance.JoinMessageVoting.Value);
        ChatApi.SendMessage(Plugin.Instance.AutoMessage.Value);
    }

    public override void Exit()
    {
        // Unsubscribe from events
        ZeepkistNetwork.PlayerResultsChanged -= OnPlayerResultsChanged;
        MultiplayerApi.PlayerJoined -= OnPlayerJoined;
        MultiplayerApi.PlayerLeft -= OnPlayerLeft;
        RacingApi.RoundEnded -= OnRoundEnded;
        FavoritePlayerChangedNotifier.FavoritePlayersChanged -= OnFavoritePlayersChanged;
        CommandVotingResult.OnHandle -= OnVotingFinished;
    }

    private void OnPlayerJoined(ZeepkistNetworkPlayer player)
    {
        UpdateVotes();
    }

    private void OnPlayerLeft(ZeepkistNetworkPlayer player)
    {
        UpdateVotes();
    }

    private void OnFavoritePlayersChanged()
    {
        UpdateVotes();
    }

    private void OnRoundEnded()
    {
        OnVotingFinished();
        StateMachine.TransitionTo(new StatePostVoting(StateMachine));
    }

    private void OnVotingFinished()
    {
        if (StateMachine.CurrentSubmissionLevel.VotesClutch < StateMachine.CurrentSubmissionLevel.VotesKick)
        {
            ChatApi.SendMessage("<br>--KICK--<br>" +
                                $"Sorry to {StateMachine.CurrentSubmissionLevel.Author} :/<br>" +
                                $"You flopped with {StateMachine.CurrentSubmissionLevel.VotesClutch} to {StateMachine.CurrentSubmissionLevel.VotesKick} votes..<br>" +
                                "You will now get kicked o7");
            ChatApi.SendMessage(ParseMessage("/servermessage red 0 " + Plugin.Instance.ResultServerMessage.Value));
        }
        else
        {
            ChatApi.SendMessage("<br>--ClUTCH--<br>" +
                                $"Congratulations to {StateMachine.CurrentSubmissionLevel.Author} :party:<br>" +
                                $"You clutched with {StateMachine.CurrentSubmissionLevel.VotesClutch} to {StateMachine.CurrentSubmissionLevel.VotesKick} votes!<br>" +
                                "Enjoy your freewin!");
            ChatApi.SendMessage(ParseMessage("/servermessage green 0 " + Plugin.Instance.ResultServerMessage.Value));
        }

        StateMachine.TransitionTo(new StatePostVoting(StateMachine));
    }

    private void OnPlayerResultsChanged(ZeepkistNetworkPlayer player)
    {
        UpdateVotes();
    }

    private void UpdateVotes()
    {
        StateMachine.CurrentSubmissionLevel.ResetVotes();
        foreach (LeaderboardItem player in ZeepkistNetwork.Leaderboard)
        {
            if (StateMachine.IsNeutral(player.SteamID))
            {
                continue;
            }

            if (player.Time >= CurrentVotingLevel.KickFinishTime)
            {
                StateMachine.CurrentSubmissionLevel.VotesKick++;
            }
            else if (player.Time >= CurrentVotingLevel.ClutchFinishTime)
            {
                StateMachine.CurrentSubmissionLevel.VotesClutch++;
            }
            else
            {
                StateMachine.KickNonNeutralPlayer(player);
            }
        }

        UpdateVotingResultsMessage();
    }


    private void UpdateVotingResultsMessage()
    {
        ChatApi.SendMessage($"/servermessage yellow 0 " +
                            $"{StateMachine.CurrentSubmissionLevel.Name} by {StateMachine.CurrentSubmissionLevel.Author}<br>" +
                            $"Kick: {Math.Max(0, StateMachine.CurrentSubmissionLevel.VotesKick)} " +
                            $"| Clutch: {Math.Max(0, StateMachine.CurrentSubmissionLevel.VotesClutch)}")
            // $"| Votes Left: {Math.Max(0, GetTotalVotes())}")
            ;
    }


    private int CountFavoritesInLobby()
    {
        return ZeepkistNetwork.Players.CountFavoritesInLobby(ZeepkistNetwork.CurrentLobby.Favorites);
    }

    private int CountNeutrals()
    {
        return 1; // Placeholder -> If sometime Neutrals come into play we will implement this. For now it "adds the host"
    }


    private string ParseMessage(string message)
    {
        return message
            .Replace("%a", StateMachine.CurrentSubmissionLevel.Author)
            .Replace("%l", StateMachine.CurrentSubmissionLevel.Name)
            .Replace("%r", VotingResultString())
            .Replace("%c", StateMachine.CurrentSubmissionLevel.VotesClutch.ToString())
            .Replace("%k", StateMachine.CurrentSubmissionLevel.VotesKick.ToString())
            .Replace("%t", GetTotalVotes().ToString());
    }

    private string VotingResultString()
    {
        return StateMachine.CurrentSubmissionLevel.VotesClutch >= StateMachine.CurrentSubmissionLevel.VotesKick ? "Clutch" : "Kick";
    }

    private int GetTotalVotes()
    {
        return CountPlayersInLobby() - CountNeutrals() - StateMachine.CurrentSubmissionLevel.VotesKick - StateMachine.CurrentSubmissionLevel.VotesClutch;
    }

    private int CountPlayersInLobby()
    {
        return ZeepkistNetwork.Players.Count;
    }
}