using System.Collections.Generic;
using UnityEngine;
using GameTemplate.Core.DI;
using GameTemplate.Core.Logger;
using GameTemplate.Core.Save;

namespace GameTemplate.Gameplay.Stats
{
    /// <summary>
    /// CharacterController - composition root cho character.
    ///
    /// Trách nhiệm:
    ///   1. Load Default (ScriptableObject) + Saved (từ disk) lúc spawn
    ///   2. Tạo EquipmentModifierSet và CharacterStats
    ///   3. Reapply equipment modifier từ Saved.EquippedItemIds
    ///   4. Save lại Saved tại checkpoint qua ISaveService
    ///
    /// MonoBehaviour wrapper - gắn lên Player GameObject.
    /// Gameplay code và UI truy cập qua property Stats.
    /// </summary>
    public class CharacterController : MonoBehaviour
    {
        [Header("Design Data")]
        [Tooltip("Default stats ScriptableObject - kéo asset DefaultStats_Warrior vào đây")]
        [SerializeField] private DefaultCharacterStats _defaults;

        [Tooltip("Database các item có thể equip - dùng để lookup khi load save")]
        [SerializeField] private List<EquipmentItem> _itemDatabase;

        // Save key trong ISaveService
        private const string SaveKey = "character";

        // Public API cho gameplay/UI truy cập
        public CharacterStats Stats { get; private set; }

        private ISaveService _saveService;

        private async void Start()
        {
            _saveService = ServiceLocator.Get<ISaveService>();

            // 1. Load saved (return SavedCharacterStats() default nếu file chưa có)
            var saved = await _saveService.LoadAsync<SavedCharacterStats>(SaveKey);

            // 2. Tạo equipment set + character stats
            var equipment = new EquipmentModifierSet();
            Stats = new CharacterStats(_defaults, saved, equipment);

            // 3. Reapply modifier từ items đang equip (load lại từ save)
            foreach (var itemId in saved.EquippedItemIds)
            {
                var item = FindItem(itemId);
                if (item == null)
                {
                    GameLog.Warning(LogCategory.Save, $"Equipped item '{itemId}' không tìm thấy trong database - skipping.");
                    continue;
                }
                foreach (var mod in item.CreateRuntimeModifiers())
                    equipment.Add(mod);
            }
            // Recompute auto qua event OnModifiersChanged

            GameLog.Info(LogCategory.Gameplay,
                $"Character loaded. Level:{Stats.Level.Value}, HP:{Stats.CurrentHp.Value}/{Stats.MaxHp.Value}, " +
                $"Attack:{Stats.Attack.Value}, Equipped:{saved.EquippedItemIds.Count} items");
        }

        // ============================================================
        // UPDATE - tick buff mỗi frame
        // ============================================================

        private void Update()
        {
            Stats?.Tick();
        }

        // ============================================================
        // CHECKPOINT SAVE - gọi từ: level complete, shop close, settings menu
        // ============================================================

        public async void SaveAtCheckpoint()
        {
            if (Stats == null) return;
            await _saveService.SaveAsync(SaveKey, Stats.Saved);
            GameLog.Info(LogCategory.Save, "Character saved at checkpoint.");
        }

        // ============================================================
        // EQUIPMENT API - gameplay gọi
        // ============================================================

        public bool EquipItem(string itemId)
        {
            var item = FindItem(itemId);
            if (item == null)
            {
                GameLog.Warning(LogCategory.Gameplay, $"Item '{itemId}' không có trong database.");
                return false;
            }
            Stats.Equip(itemId, item.CreateRuntimeModifiers());
            return true;
        }

        public bool UnequipItem(string itemId)
        {
            Stats.Unequip(itemId);
            return true;
        }

        // ============================================================
        // BUFF API - gameplay gọi từ skill, potion, aura zone
        // ============================================================

        /// <summary>Apply 1 buff lên character. Nếu đã có cùng buffId → refresh hoặc stack.</summary>
        public void ApplyBuff(BuffDefinition def)
        {
            if (Stats == null || def == null) return;
            Stats.ApplyBuff(def);
        }

        /// <summary>Remove buff thủ công (skill dispel, hết aura zone).</summary>
        public bool RemoveBuff(string buffId)
            => Stats != null && Stats.RemoveBuff(buffId);

        /// <summary>Clear hết buff (vd: death, scene transition).</summary>
        public void ClearAllBuffs() => Stats?.ClearAllBuffs();

        // ============================================================
        // COMBAT NOTIFY - gameplay gọi để fire trigger buff
        // (Vampiric, Thorns, OnKill regen...)
        // ============================================================

        /// <summary>Gọi khi character này đánh enemy thành công.</summary>
        public void OnAttackPerformed(CharacterController target, int damageDealt)
        {
            Stats?.NotifyAttack(target?.Stats, damageDealt);
        }

        /// <summary>Gọi khi character này nhận damage. Đặt SAU TakeDamage trong code combat.</summary>
        public void OnDamageReceived(CharacterController attacker, int damageTaken)
        {
            Stats?.NotifyDamaged(attacker?.Stats, damageTaken);
        }

        /// <summary>Gọi khi character này giết enemy.</summary>
        public void OnEnemyKilled(CharacterController victim)
        {
            Stats?.NotifyKill(victim?.Stats);
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        private void OnDestroy() => Stats?.Dispose();

        private EquipmentItem FindItem(string itemId)
        {
            foreach (var item in _itemDatabase)
                if (item != null && item.ItemId == itemId) return item;
            return null;
        }
    }
}
