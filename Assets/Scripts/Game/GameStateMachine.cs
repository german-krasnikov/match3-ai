using System;

namespace Match3.Game
{
    /// <summary>
    /// Simple finite state machine for game loop.
    /// Does NOT contain game logic — only state management.
    /// </summary>
    public class GameStateMachine
    {
        private GameState _currentState;

        /// <summary>
        /// Current game state.
        /// </summary>
        public GameState CurrentState => _currentState;

        /// <summary>
        /// Fires when state changes.
        /// Parameters: (previousState, newState)
        /// </summary>
        public event Action<GameState, GameState> OnStateChanged;

        /// <summary>
        /// Creates FSM starting in Idle state.
        /// </summary>
        public GameStateMachine()
        {
            _currentState = GameState.Idle;
        }

        /// <summary>
        /// Creates FSM with specified initial state.
        /// </summary>
        public GameStateMachine(GameState initialState)
        {
            _currentState = initialState;
        }

        /// <summary>
        /// Transitions to new state. Fires OnStateChanged.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (_currentState == newState)
                return;

            var previousState = _currentState;
            _currentState = newState;

            OnStateChanged?.Invoke(previousState, newState);
        }

        /// <summary>
        /// Checks if current state matches expected.
        /// </summary>
        public bool IsInState(GameState state)
        {
            return _currentState == state;
        }

        /// <summary>
        /// Resets FSM to Idle state.
        /// </summary>
        public void Reset()
        {
            SetState(GameState.Idle);
        }
    }
}
