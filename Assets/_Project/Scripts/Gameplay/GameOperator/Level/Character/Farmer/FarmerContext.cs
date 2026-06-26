using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class FarmerContext
    {
        // ── Dependencies ──────────────────────────────────────────────────
        public readonly PathFinding PathfindingService;
        public readonly CharacterStateMachine StateMachine;
        public readonly ContructionController ContructionBehavior;
        public readonly LevelController LevelController;

        // ── Vị trí động ───────────────────────────────────────────────────
        public Func<Vector3> TreePosition;
        public Func<Vector3> GuestPosition;

        // ── Data runtime ──────────────────────────────────────────────────
        public GuestBehavior CurrentGuest { get; set; }
        public Transform[] FetchedItems { get; set; }

        // ─────────────────────────────────────────────────────────────────
        public FarmerContext(
            PathFinding pathfindingService,
            CharacterStateMachine stateMachine,
            ContructionController treeBehavior,
            LevelController levelController)
        {
            PathfindingService = pathfindingService;
            StateMachine       = stateMachine;
            ContructionBehavior = treeBehavior;
            LevelController    = levelController;
        }
    }
}
