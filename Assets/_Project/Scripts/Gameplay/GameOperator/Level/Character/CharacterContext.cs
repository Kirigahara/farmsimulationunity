using System.Collections.Generic;
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
        public Action ResetTransform;
        public Action DeSpawn;

        //-------Animation-------
        public Action PlayIdle;
        public Action PlayMove;
        public Action PlayIdleCarry;
        public Action PlayMoveCarry;

        [Header("Product Position")]
        public Transform ProductGroup;
        public List<ProductionController> Productions;

        [Header("Data")]
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
