using System.Collections.Generic;
using ZeepkistClient;
using ZeepSDK.Racing;

namespace KoC.states;

public class StatePostVoting(KoC koC) : BaseState(koC)
{
    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;

        foreach (KeyValuePair<ulong, float> keyValuePair in koC.OriginalVoteTime)
        {
            ZeepkistNetwork.CustomLeaderBoard_SetPlayerLeaderboardOverrides(keyValuePair.Key);
            ZeepkistNetwork.CustomLeaderBoard_SetPlayerTimeOnLeaderboard(keyValuePair.Key, keyValuePair.Value, false);
        }

        koC.OriginalVoteTime = new Dictionary<ulong, float>();
        ZeepkistNetwork.PlayerResultsChanged += OnPlayerResultsChanged;
    }

    private void OnLevelLoaded()
    {
        KoC.TransitionTo(new StateRegisterSubmission(KoC));
    }


    private void OnPlayerResultsChanged(ZeepkistNetworkPlayer player)
    {
        if (player.CurrentResult?.Time < KoC.CurrentVotingLevel.ClutchFinishTime)
        {
            KoC.RemoveFromLeaderBoardIfNotNeutral(player);
        }
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
        ZeepkistNetwork.PlayerResultsChanged -= OnPlayerResultsChanged;
    }
}