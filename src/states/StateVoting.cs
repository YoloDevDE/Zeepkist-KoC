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
        // Assemble the final message
        string resultServerMessage =

            $"/servermessage white 0 <align=\"left\"><margin-left=\"50%\"><size=\"30%\"><br><br>" +
            $"<#ff9900>{KoC.SubmissionLevel.Name} <#ffffff>by <#ff9900>{KoC.SubmissionLevel.Author}<#ffffff>";
        if (KoC.SubmissionLevel.VotesClutch < KoC.SubmissionLevel.VotesKick)
        {
            ChatApi.SendMessage("<br>--KICK--<br>" +
                                $"Sorry to {KoC.SubmissionLevel.Author} :/<br>" +
                                $"You flopped with {KoC.SubmissionLevel.VotesClutch} to {KoC.SubmissionLevel.VotesKick} votes..<br>" +
                                "You will now get kicked o7");
            resultServerMessage += " got <#ff0000>KICKED";
        }
        else
        {
            ChatApi.SendMessage("<br>--ClUTCH--<br>" +
                                $"Congratulations to {KoC.SubmissionLevel.Author} :party:<br>" +
                                $"You clutched with {KoC.SubmissionLevel.VotesClutch} to {KoC.SubmissionLevel.VotesKick} votes!<br>" +
                                "Enjoy your freewin!");
           
            resultServerMessage += " got <#00ff00>CLUTCHED";
        }

        
        ChatApi.SendMessage(resultServerMessage + "<br><br><br><br><br>");

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
    int totalVotes = KoC.SubmissionLevel.VotesKick + KoC.SubmissionLevel.VotesClutch;

    // Calculate the ratio for clutch and kick votes
    double clutchRatio = totalVotes > 0 ? (double)KoC.SubmissionLevel.VotesClutch / totalVotes : 0.5;
    int indicatorLength = 15;
    // Calculate the indicator position relative to the total dots (scale to 22 positions)
    int indicatorPosition = (int)Math.Round(clutchRatio * (2 * (indicatorLength + 1)));

    // Insert the moving indicator at the calculated position
    string movingIndicator = new string(' ', indicatorPosition) + "^";
    string dots = new string('.', indicatorLength);
    // Format the votes to always display as two digits

    // Format the votes to always display at least two characters, padded with spaces
    string votesKickFormatted = KoC.SubmissionLevel.VotesKick.ToString().PadLeft(2, ' ').PadLeft(indicatorLength+1 - "Kick -> ".Length);
    string votesClutchFormatted = KoC.SubmissionLevel.VotesClutch.ToString().PadRight(2, ' ').PadRight(indicatorLength+1 - " <- Clutch".Length);
    // Assemble the final message
    ChatApi.SendMessage($"/servermessage white 0 <align=\"left\"><margin-left=\"50%\"><size=\"30%\"><br><br>" +
                        $"<#ff9900>{KoC.SubmissionLevel.Name} <#ffffff>by <#ff9900>{KoC.SubmissionLevel.Author}<br><br><#ffffff>" +
                        $"<#ffffff>Kick -> <#ff0000>{votesKickFormatted}<#ffffff>|<#00ff00>{votesClutchFormatted}<#ffffff> <- Clutch<br>" +
                        $"<#ffffff>|<#ff0000>{dots}<#ffffff>|<#00ff00>{dots}<#ffffff>|<br>" +
                        $"{movingIndicator}");
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