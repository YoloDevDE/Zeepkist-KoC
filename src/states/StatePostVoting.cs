using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Racing;

namespace KoC.states;

public class StatePostVoting(KoC koC) : BaseState(koC)
{
    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
        ZeepkistNetwork.PlayerResultsChanged += OnPlayerResultsChanged;
    }

    private void OnLevelLoaded()
    {
        KoC.TransitionTo(new StateRegisterSubmission(KoC));
    }


    private void OnPlayerResultsChanged(ZeepkistNetworkPlayer player)
    {
        foreach (LeaderboardItem item in ZeepkistNetwork.Leaderboard)
        {
            if (item.Time < KoC.CurrentVotingLevel.ClutchFinishTime)
            {
                KoC.KickIfNotNeutralPlayer(item);
            }
        }
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
        ZeepkistNetwork.PlayerResultsChanged -= OnPlayerResultsChanged;
    }
}