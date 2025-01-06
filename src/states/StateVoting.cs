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
                                "Enjoy your free win!");

            resultServerMessage += " got <#00ff00>CLUTCHED";
        }


        ChatApi.SendMessage(resultServerMessage + "<br><br><br><br><br>");

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
            UpdateVotingResultsMessage();
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

        UpdateVotingResultsMessage();
    }

    

    private void UpdateVotingResultsMessage()
    {
        int totalVotes = KoC.SubmissionLevel.VotesKick + KoC.SubmissionLevel.VotesClutch;

        // Calculate clutch and kick ratios (sum is always 1)
        double clutchRatio = totalVotes > 0 ? (double)KoC.SubmissionLevel.VotesClutch / totalVotes : 0.5;
        double kickRatio = 1.0 - clutchRatio; // complementary ratio

        int indicatorLength = 15; // total length of the indicator in dots

        // Determine the length of each colored segment based on the ratios
        int clutchDots = (int)Math.Round(clutchRatio * indicatorLength * 2);
        int kickDots = indicatorLength * 2 - clutchDots;
        string dots = new string(' ', indicatorLength - 1);
        string clutchPadding = new string(' ', Math.Max(0, clutchDots - (clutchRatio <= 0.5 ? 0 : 1)));
        // Construct the line with green on the left and red on the right
        string ratioBar =
            "<mark=#00ff00AA>" +
            new string('.', clutchDots) +
            "</mark>" + // Green for clutch
            "<mark=#ff0000AA>" +
            new string('.', kickDots) +
            "</mark>"; // Red for kick
        if (totalVotes == 0)
        {
            ratioBar = "";
        }

        string votesKickFormatted = KoC.SubmissionLevel.VotesKick.ToString().PadLeft(indicatorLength);
        string votesClutchFormatted = KoC.SubmissionLevel.VotesClutch.ToString();


        string kickText = "Kick" + "Clutch".PadLeft(dots.Length * 2 - 2);
        ChatApi.SendMessage(
                $"/servermessage white 0 <align=\"left\"><margin-left=\"50%\"><size=\"30%\"><br><br>" +
                $"<#ff9900>{KoC.SubmissionLevel.Name} <#ffffff>by <#ff9900>{KoC.SubmissionLevel.Author}<br><br><#ffffff>" +
                $"<#ffffff>{kickText}" +
                $"<pos=0><#ff0000>{votesKickFormatted}<#ffffff>|<#00ff00>{votesClutchFormatted}<#ffffff><br>" +
                $"<pos=0>{ratioBar}" +
                $"<pos=0><#ffffff>|{dots}{dots}|" +
                $"<pos=0><#ffffff>{dots}|" +
                $"<pos=0><voffset=-.90em><#ffffff>{clutchPadding}^</voffset>" +
                $"<pos=0><#ffffff>{clutchPadding}|" +
                $"<pos=0><voffset=.40em><rotate=180><#ffffff>{clutchPadding}^</rotate></voffset>"
            )
            ;
    }
}