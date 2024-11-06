using ZeepkistClient;
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