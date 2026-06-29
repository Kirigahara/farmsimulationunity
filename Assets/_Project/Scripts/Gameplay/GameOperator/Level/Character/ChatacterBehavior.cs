using GameTemplate.Core.Patterns.Factory;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class ChatacterBehavior : MonoBehaviour
    {
        public FactoryDataBase _DefaultData;
        public Animator _Animator;
        public Transform _ProductGroup;
        protected CharacterStat2 _CharacterStat;

        public PathNode _CurrentPathNode;
        //protected PathNode _FinishNode;

        public void UpdatePosition(Vector3 position)
        {
            this.transform.position = position;
        }
        public void UpdateRotation(Quaternion rotation) 
        {
            this.transform.rotation = rotation;
        }

        private void SetAnimState(bool isMove, bool isCarryMove, bool isEmpty)
        {
            _Animator.SetBool(CharacterAnimParam.IsMove, isMove);
            _Animator.SetBool(CharacterAnimParam.IsCarryMove, isCarryMove);
            _Animator.SetBool(CharacterAnimParam.IsEmpty, isEmpty);
        }

        public void PlayIdle() => SetAnimState(false, false, true);
        public void PlayIdleCarry() => SetAnimState(false, false, false);
        public void PlayMove() => SetAnimState(true, false, true);
        public void PlayMoveCarry() => SetAnimState(true, true, false);
    }

    public class CharacterAnimParam
    {
        public const string IsMove = "IsMove";
        public const string IsCarryMove = "IsCarryMove";
        public const string IsEmpty = "IsEmpty";
    }
}
