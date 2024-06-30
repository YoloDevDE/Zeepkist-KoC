using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Racing;

namespace KoC.states;

public class StatePostVoting(StateMachine stateMachine) : BaseState(stateMachine)
{
    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
        ZeepkistNetwork.PlayerResultsChanged += OnPlayerResultsChanged;
    }

    private void OnLevelLoaded()
    {
        StateMachine.TransitionTo(new StateRegisterSubmission(StateMachine));
    }


    private void OnPlayerResultsChanged(ZeepkistNetworkPlayer player)
    {
        foreach (LeaderboardItem item in ZeepkistNetwork.Leaderboard)
        {
            if (item.Time < StateMachine.CurrentVotingLevel.ClutchFinishTime)
            {
                StateMachine.KickIfNotNeutralPlayer(item);
            }
        }
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
        ZeepkistNetwork.PlayerResultsChanged -= OnPlayerResultsChanged;
    }
}