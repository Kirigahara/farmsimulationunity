using System;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class GuestContext
    {
        // ── Dependencies ──────────────────────────────────────────────────
        public readonly PathFinding PathfindingService;
        public readonly CharacterStateMachine StateMachine;

        // ── Vị trí động ───────────────────────────────────────────────────
        public Func<Vector3> CounterPosition;
        public Func<Vector3> DespawnPosition;

        // ── Data ──────────────────────────────────────────────────────────
        public string ItemToBuy { get; set; }

        // ─────────────────────────────────────────────────────────────────
        public GuestContext(PathFinding pathfindingService, CharacterStateMachine stateMachine)
        {
            PathfindingService = pathfindingService;
            StateMachine       = stateMachine;
        }
    }
}
