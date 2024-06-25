using System.Collections.Generic;
using System.Linq;
using KoC.Data;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Messaging;
using ZeepSDK.Racing;

namespace KoC.States;

public class StatePostVoting : BaseState
{
    public StatePostVoting(StateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
        ZeepkistNetwork.PlayerResultsChanged += OnPlayerResultsChanged;
    }

    private void OnLevelLoaded()
    {
        StateMachine.TransitionTo(new StateRegisterSubmission(StateMachine));
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

        MessengerApi.LogError(
            "No Voting-Level found! To set a Voting-Level go to a custom Voting-Level and type '/koc save'.", 5F);
        return null;
    }

    private bool IsFavorite(ulong steamID)
    {
        if (steamID == ZeepkistNetwork.LocalPlayer.SteamID)
        {
            return true;
        }

        if (steamID == StateMachine.CurrentSubmissionLevel.AuthorSteamId)
        {
            return true;
        }

        return ZeepkistNetwork.CurrentLobby.Favorites.Contains(steamID);
    }

    private void OnPlayerResultsChanged(ZeepkistNetworkPlayer player)
    {
        VotingLevel currentVotingLevel = FetchVotingLevel(Plugin.Instance.GetVotingLevels());
        foreach (LeaderboardItem item in ZeepkistNetwork.Leaderboard)
        {
            if (item.Time < currentVotingLevel.ClutchFinishTime)
            {
                if (!IsFavorite(item.SteamID))
                {
                    ZeepkistNetworkPlayer zeepkistNetworkPlayer =
                        ZeepkistNetwork.PlayerList.FirstOrDefault(x => x.SteamID == item.SteamID);
                    ZeepkistNetwork.KickPlayer(zeepkistNetworkPlayer);
                }
            }
        }
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
        ZeepkistNetwork.PlayerResultsChanged -= OnPlayerResultsChanged;
    }
}