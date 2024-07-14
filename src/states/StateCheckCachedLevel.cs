using KoC.utils;
using ZeepkistClient;

namespace KoC.states;

public class StateCheckCachedLevel(StateMachine stateMachine) : BaseState(stateMachine)
{
    public override void Enter()
    {
        string levelUid = ZeepkistNetwork.CurrentLobby.LevelUID;

        if (LevelUtils.IsVotingLevel(levelUid, StateMachine.VotingLevels) && StateMachine.SubmissionLevel == null)
        {
            StateMachine.SubmissionLevel = StateMachine.CachedSubmissionLevel;
            StateMachine.TransitionTo(new StateVoting(StateMachine));
        }
        else
        {
            StateMachine.TransitionTo(new StateRegisterSubmission(StateMachine));
        }
    }

    public override void Exit()
    {
    }
}