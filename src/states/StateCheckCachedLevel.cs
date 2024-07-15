using KoC.utils;
using ZeepkistClient;

namespace KoC.states;

public class StateCheckCachedLevel(KoC koC) : BaseState(koC)
{
    public override void Enter()
    {
        string levelUid = ZeepkistNetwork.CurrentLobby.LevelUID;

        if (LevelUtils.IsVotingLevel(levelUid, KoC.VotingLevels) && KoC.SubmissionLevel == null)
        {
            KoC.SubmissionLevel = KoC.CachedSubmissionLevel;
            KoC.TransitionTo(new StateVoting(KoC));
        }
        else
        {
            KoC.TransitionTo(new StateRegisterSubmission(KoC));
        }
    }

    public override void Exit()
    {
    }
}