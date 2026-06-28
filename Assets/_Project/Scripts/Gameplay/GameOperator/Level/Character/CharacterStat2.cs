using GameTemplate.Gameplay.Stats;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class CharacterStat2
    {
        BuffSet _BuffSet;
        public float _MoveSpeed = 1.0f;

        public float MoveSpeed => _MoveSpeed * (1 + _BuffSpeed);
        public BuffSet BuffSet => _BuffSet;

        float _BuffSpeed = 0.0f;

        public CharacterStat2() 
        {
            _BuffSet = new BuffSet(
                onTickDamage: ApplyTickSpeed,
                onTriggerFired: HandleTriggerFired);
        }

        /// <summary>Callback BuffSet gọi mỗi tick (DoT/HoT).</summary>
        private void ApplyTickSpeed(int amount)
        {
            _BuffSpeed = ((float)amount) / 100;
        }

        // ============================================================
        // TRIGGER HANDLER - xử lý effect khi trigger fire
        // ============================================================
        private void HandleTriggerFired(TriggerContext ctx, BuffInstance buff)
        {

        }
    }
}
