namespace FSM
{
    public class BattleStateMachine
    {
        public IBattleState CurrentState { get; private set; }

        public void TransitionTo(IBattleState newState)
        {
            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState?.Enter();
        }

        public void Update()
        {
            CurrentState?.Update();
        }
    }
}
