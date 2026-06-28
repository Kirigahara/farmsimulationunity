using System;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public abstract class CharacterContext
    {
        public readonly PathFollower PathFollower;
        public readonly CharacterStateMachine StateMachine;

        public Func<Vector3> Coordinate;
        public Action<Vector3> UpdatePosition;
        public Action<Quaternion> UpdateRotation;
        public Action OnReachedCounter;

        public CharacterStat2 Stat { get; set; }
        public PathNode CurrentNode;
        public PathNode FinishNode;

        protected CharacterContext(PathFollower pathFollower, CharacterStateMachine stateMachine)
        {
            PathFollower = pathFollower;
            StateMachine = stateMachine;
        }
    }
}
