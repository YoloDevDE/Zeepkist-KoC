using KoC.Commands;
using KoC.Data;
using KoC.States;
using ZeepSDK.Multiplayer;

namespace KoC;

public class StateMachine
{
    private BaseState _baseState;
    public Plugin Plugin;
    public SubmissionLevel SubmissionLevel;

    public StateMachine(Plugin plugin)
    {
        Plugin = plugin;
        _baseState = new StateOffline(this);
        _baseState.Enter();
        ModStop.OnHandle += _baseState.Kill;
        ModStart.OnHandle += _baseState.Start;
        MultiplayerApi.DisconnectedFromGame += _baseState.KillOnError;
    }

    public void SwitchState(BaseState baseState)
    {
        ModStop.OnHandle -= _baseState.Kill;
        ModStart.OnHandle -= _baseState.Start;
        MultiplayerApi.DisconnectedFromGame -= _baseState.KillOnError;
        _baseState.Exit();
        _baseState = baseState;
        _baseState.Enter();
        ModStop.OnHandle += _baseState.Kill;
        ModStart.OnHandle += _baseState.Start;
        MultiplayerApi.DisconnectedFromGame += _baseState.KillOnError;
    }

    public void KillSwitch()
    {
        SwitchState(new StateOffline(this));
    }
}