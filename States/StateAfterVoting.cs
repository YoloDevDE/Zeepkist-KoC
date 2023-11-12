using System;
using System.Collections.Generic;
using System.Linq;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.Messaging;
using ZeepSDK.Racing;

namespace KoC.States;

public class StateAfterVoting : BaseState
{
    public StateAfterVoting(StateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        RacingApi.LevelLoaded += RacingApiOnLevelLoaded;
        ZeepkistNetwork.PlayerResultsChanged += PlayerResultsChanged;
        
    }

    private void RacingApiOnLevelLoaded()
    {
        StateMachine.SwitchState(new StateRegisterSubmission(StateMachine));
    }

    private VotingLevel FetchVotingLevel(List<VotingLevel> votingLevels)
    {
        foreach (var votingLevel in votingLevels)
            if (votingLevel.LevelUid == PlayerManager.Instance.currentMaster.GlobalLevel.UID)
                return votingLevel;

        MessengerApi.LogError(
            "No Voting-Level found! To set a Voting-Level go to a custom Voting-Level and type '/koc save'.", 5F);
        return null;
    }
    private bool IsFavorite(ulong steamID)
    {
        if (steamID == ZeepkistNetwork.LocalPlayer.SteamID)
            return true;
        if (steamID == StateMachine.SubmissionLevel.AuthorSteamId)
            return true;
        return ZeepkistNetwork.CurrentLobby.Favorites.Contains(steamID);
    }
    private void PlayerResultsChanged(ZeepkistNetworkPlayer player)
    {
        var currentVotingLevel = FetchVotingLevel(StateMachine.Plugin.GetVotingLevels());
        foreach (var item in ZeepkistNetwork.Leaderboard)
        {

            if (item.Time < currentVotingLevel.ClutchFinishTime)
            {
                if (!IsFavorite(item.SteamID))
                {
                    var zeepkistNetworkPlayer =
                        ZeepkistNetwork.PlayerList.FirstOrDefault(x => x.SteamID == item.SteamID);
                    ZeepkistNetwork.KickPlayer(zeepkistNetworkPlayer);
                }
            }
        }
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= RacingApiOnLevelLoaded;
        ZeepkistNetwork.PlayerResultsChanged -= PlayerResultsChanged;
    }
}