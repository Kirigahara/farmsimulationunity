using System;
using System.Collections.Generic;
using GameTemplate.Core.Save;

namespace GameTemplate.Gameplay.Stats
{
    /// <summary>
    /// SavedCharacterStats - chỉ số ĐÃ NÂNG CẤP của player (persist xuống disk).
    ///
    /// Đặc điểm:
    ///   - Chứa DELTA so với default (level, stat point đầu tư) - KHÔNG lưu absolute
    ///   - Vì sao? Update game đổi default → save cũ vẫn dùng được, không bị stale
    ///   - Save tại CHECKPOINT (level complete, shop close, settings menu) qua ISaveService
    ///   - Serializable bằng JsonUtility: dùng [Serializable], public field
    ///
    /// Lưu ý:
    ///   - KHÔNG dùng trực tiếp cho UI - phải compute qua CharacterStats trước
    ///   - Save chứa progression, KHÔNG chứa "Hp hiện tại" - đó là runtime state
    ///   - Method trên class này đảm bảo invariant (vd: AddExp tự handle level up)
    /// </summary>
    [Serializable]
    public class SavedCharacterStats : SaveDataBase
    {
        // Reference tới default đã chọn - load đúng template khi game start
        public string CharacterId = "warrior";

        // Progression
        public int Level = 1;
        public int CurrentExp = 0;

        // Stat point đã đầu tư (player tự distribute khi level up)
        public int InvestedHp = 0;
        public int InvestedAttack = 0;
        public int InvestedDefense = 0;

        // Stat point chưa dùng - dồn khi level up
        public int UnspentStatPoints = 0;

        // Permanent bonus từ shop/quest reward (không reset khi respec)
        public int PermanentHpBonus = 0;
        public int PermanentAttackBonus = 0;

        // ItemId đang equip - load lên sẽ reapply modifier vào EquipmentModifierSet
        public List<string> EquippedItemIds = new List<string>();

        // ============================================================
        // API CHO GAMEPLAY - đảm bảo invariant
        // ============================================================

        /// <summary>Add EXP, return true nếu level up.</summary>
        public bool AddExp(int amount, DefaultCharacterStats defaults)
        {
            if (Level >= defaults.MaxLevel) return false;

            CurrentExp += amount;
            bool leveledUp = false;

            while (CurrentExp >= GetExpToNextLevel())
            {
                CurrentExp -= GetExpToNextLevel();
                Level++;
                UnspentStatPoints += 3; // 3 point/level
                leveledUp = true;

                if (Level >= defaults.MaxLevel)
                {
                    CurrentExp = 0;
                    break;
                }
            }
            return leveledUp;
        }

        public int GetExpToNextLevel() => 100 * Level * Level;

        /// <summary>Đầu tư 1 stat point. Return false nếu hết point.</summary>
        public bool SpendStatPoint(StatType type)
        {
            if (UnspentStatPoints <= 0) return false;

            switch (type)
            {
                case StatType.Hp:      InvestedHp++; break;
                case StatType.Attack:  InvestedAttack++; break;
                case StatType.Defense: InvestedDefense++; break;
                default: return false;
            }
            UnspentStatPoints--;
            return true;
        }

        /// <summary>Hoàn lại stat point đã đầu tư (cho feature respec).</summary>
        public void ResetStatPoints()
        {
            UnspentStatPoints += InvestedHp + InvestedAttack + InvestedDefense;
            InvestedHp = InvestedAttack = InvestedDefense = 0;
        }

        // ===== Equipment tracking =====
        public void EquipItem(string itemId)
        {
            if (!EquippedItemIds.Contains(itemId))
                EquippedItemIds.Add(itemId);
        }

        public void UnequipItem(string itemId) => EquippedItemIds.Remove(itemId);
    }

    public enum StatType
    {
        Hp,
        Attack,
        Defense,
        MoveSpeed,
        CritRate,
    }
}
