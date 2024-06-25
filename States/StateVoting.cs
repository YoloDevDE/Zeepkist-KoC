using System;
using System.Collections.Generic;
using System.Linq;
using KoC.Commands;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Chat;
using ZeepSDK.Messaging;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace KoC.States;

public class StateVoting : BaseState
{
    private VotingLevel _currentVotingLevel;
    private int _voteNo;
    private int _voteTotal;
    private int _voteYes;


    public StateVoting(StateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Exit()
    {
        ZeepkistNetwork.PlayerResultsChanged -= PlayerResultsChanged;
        MultiplayerApi.PlayerJoined -= PlayerResultsChanged;
        MultiplayerApi.PlayerLeft -= PlayerResultsChanged;
        CommandVotingResult.OnHandle -= OnVotingResult;
        RacingApi.RoundEnded -= OnGameStateChange;

        ChatApi.SendMessage("/joinmessage orange " + StateMachine.Plugin.JoinMessageNormal.Value);
    }

    public override void Enter()
    {
        _currentVotingLevel = FetchVotingLevel(StateMachine.Plugin.GetVotingLevels());
        ZeepkistNetwork.PlayerResultsChanged += PlayerResultsChanged;
        MultiplayerApi.PlayerJoined += PlayerResultsChanged;
        MultiplayerApi.PlayerLeft += PlayerResultsChanged;
        CommandVotingResult.OnHandle += OnVotingResult;
        RacingApi.RoundEnded += OnGameStateChange;
        PlayerResultsChanged(null);
        ChatApi.SendMessage(StateMachine.Plugin.AutoMessage.Value);
        ChatApi.SendMessage("/joinmessage orange " + StateMachine.Plugin.JoinMessageVoting.Value);
    }

    private void OnGameStateChange()
    {
        OnVotingResult();
        StateMachine.SwitchState(new StateAfterVoting(StateMachine));
    }

    private void OnVotingResult()
    {
        if (_voteYes >= _voteNo)
        {
            ChatApi.SendMessage(ParseMessage(StateMachine.Plugin.ClutchMessage.Value));
            ChatApi.SendMessage(ParseMessage("/servermessage green 0 " +
                                             StateMachine.Plugin.ResultServerMessage.Value));
        }
        else
        {
            ChatApi.SendMessage(ParseMessage(StateMachine.Plugin.KickMessage.Value));
            ChatApi.SendMessage(ParseMessage("/servermessage red 0 " + StateMachine.Plugin.ResultServerMessage.Value));
        }

        StateMachine.SwitchState(new StateAfterVoting(StateMachine));
    }

    private VotingLevel FetchVotingLevel(List<VotingLevel> votingLevels)
    {
        foreach (VotingLevel votingLevel in votingLevels)
        {
            if (votingLevel.LevelUid == PlayerManager.Instance.currentMaster.GlobalLevel.UID)
            {
                return votingLevel;
            }
        }

        MessengerApi.LogError("No Voting-Level found! To set a Voting-Level go to a custom Voting-Level and type '/koc save'.", 5F);
        return null;
    }

    private int GetNeutrals()
    {
        var count = 0;
        foreach ((uint key, ZeepkistNetworkPlayer zeepkistNetworkPlayer) in ZeepkistNetwork.Players)
        {
            if (ZeepkistNetwork.CurrentLobby.Favorites.Contains(zeepkistNetworkPlayer.SteamID))
            {
                count++;
            }
        }
        return count;
    }

    private bool IsFavorite(ulong steamID)
    {
        if (steamID == ZeepkistNetwork.LocalPlayer.SteamID)
        {
            return true;
        }

        if (steamID == StateMachine.SubmissionLevel.AuthorSteamId)
        {
            return true;
        }

        return ZeepkistNetwork.CurrentLobby.Favorites.Contains(steamID);
    }

    private string ParseMessage(string message)
    {
        return message
                .Replace("%a", StateMachine.SubmissionLevel.Author)
                .Replace("%l", StateMachine.SubmissionLevel.Name)
                .Replace("%r", VoteResult())
                .Replace("%c", _voteYes.ToString())
                .Replace("%k", _voteNo.ToString())
                .Replace("%t", _voteTotal.ToString())
            ;
    }

    private string VoteResult()
    {
        return _voteYes >= _voteNo ? "Clutch" : "Kick";
    }


    private void PlayerResultsChanged(ZeepkistNetworkPlayer player)
    {
        _voteYes = 0;
        _voteNo = 0;
        _voteTotal = ZeepkistNetwork.Players.Count - GetNeutrals();

        _currentVotingLevel = FetchVotingLevel(StateMachine.Plugin.GetVotingLevels());
        foreach (LeaderboardItem item in ZeepkistNetwork.Leaderboard)
        {
            if (item.Time >= _currentVotingLevel.KickFinishTime)
            {
                _voteNo += 1;
                _voteTotal -= 1;
            }
            else if (item.Time >= _currentVotingLevel.ClutchFinishTime)
            {
                _voteYes += 1;
                _voteTotal -= 1;
            }
            else
            {
                if (!IsFavorite(item.SteamID))
                {
                    ZeepkistNetworkPlayer zeepkistNetworkPlayer =
                        ZeepkistNetwork.PlayerList.FirstOrDefault(x => x.SteamID == item.SteamID);
                    ZeepkistNetwork.KickPlayer(zeepkistNetworkPlayer);
                }
            }
        }


        ChatApi.SendMessage(
            $"/servermessage yellow 0 {StateMachine.SubmissionLevel.Name} by {StateMachine.SubmissionLevel.Author}<br>  Kick: {Math.Max(0, _voteNo)} | Clutch: {Math.Max(0, _voteYes)} | Votes Left: {Math.Max(0, _voteTotal)} ");
    }
}