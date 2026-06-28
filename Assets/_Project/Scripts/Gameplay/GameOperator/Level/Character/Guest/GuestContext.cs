using GameTemplate.Gameplay.Stats;
using System;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class GuestContext : CharacterContext
    {
        // ── Vị trí động riêng ─────────────────────────────────────────────
        public Func<Vector3> CounterPosition;
        public Func<Vector3> DespawnPosition;

        // ── Data riêng ────────────────────────────────────────────────────
        public ContructionController ItemToBuy { get; set; }

        // ─────────────────────────────────────────────────────────────────
        public GuestContext(PathFollower pathFollower, CharacterStateMachine stateMachine)
            : base(pathFollower, stateMachine) { }
    }
}
