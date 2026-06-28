using GameTemplate.Gameplay.Stats;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class FarmerContext : CharacterContext
    {
        // ── Dependencies riêng ────────────────────────────────────────────
        public readonly ContructionController ContructionBehavior;
        public readonly LevelController LevelController;

        // ── Vị trí động riêng ─────────────────────────────────────────────
        public Func<Vector3> TreePosition;
        public Func<Vector3> GuestPosition;

        // ── Data runtime riêng ────────────────────────────────────────────
        public GuestBehavior CurrentGuest { get; set; }
        public Transform[] FetchedItems { get; set; }

        // ─────────────────────────────────────────────────────────────────
        public FarmerContext(
            PathFollower pathFollower,
            CharacterStateMachine stateMachine,
            ContructionController contructionBehavior,
            LevelController levelController)
            : base(pathFollower, stateMachine)
        {
            ContructionBehavior = contructionBehavior;
            LevelController = levelController;
        }
    }
}
