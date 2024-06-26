using ZeepkistClient;
using ZeepkistNetworking;
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


    private void OnPlayerResultsChanged(ZeepkistNetworkPlayer player)
    {
        foreach (LeaderboardItem item in ZeepkistNetwork.Leaderboard)
        {
            if (item.Time < StateMachine.CurrentVotingLevel.ClutchFinishTime)
            {
                StateMachine.KickNonNeutralPlayer(item);
            }
        }
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
        ZeepkistNetwork.PlayerResultsChanged -= OnPlayerResultsChanged;
    }
}