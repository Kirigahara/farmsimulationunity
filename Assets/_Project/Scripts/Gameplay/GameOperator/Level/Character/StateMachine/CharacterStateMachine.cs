using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class CharacterStateMachine
    {
        private ICharacterState _currentState;

        public ICharacterState CurrentState => _currentState;

        public void ChangeState(ICharacterState newState)
        {
            if (newState == null)
            {
                Debug.LogWarning("[CharacterStateMachine] ChangeState nhận null — bỏ qua.");
                return;
            }

            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        public void Update()
        {
            _currentState?.Execute();
        }
    }
}
