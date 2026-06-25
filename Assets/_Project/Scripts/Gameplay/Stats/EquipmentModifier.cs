using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Gameplay.Stats
{
    /// <summary>
    /// EquipmentModifier - cộng stats từ 1 item đang equip (weapon, armor, accessory).
    ///
    /// Đặc điểm:
    ///   - Permanent trong khi equip (không expire theo thời gian)
    ///   - Identify bằng Source (vd: "weapon_iron_sword", "armor_plate_lv5")
    ///   - Có 2 kind: Flat (+20 HP) và Percent (+15% Attack)
    ///   - KHÔNG persist riêng - persist qua danh sách "ItemId đang equip" trong SavedCharacterStats
    ///     rồi load lên reapply
    /// </summary>
    [Serializable]
    public class EquipmentModifier
    {
        public string Source;     // unique key (item id)
        public StatType Type;
        public ModifierKind Kind;
        public float Value;
    }

    public enum ModifierKind
    {
        Flat,       // +20 HP
        Percent,    // +15% Attack
    }

    /// <summary>
    /// Quản lý modifier đang active từ equipment.
    /// Tách thành class riêng để CharacterStats gọn + test được không cần Unity Editor.
    /// </summary>
    public class EquipmentModifierSet
    {
        private readonly List<EquipmentModifier> _modifiers = new List<EquipmentModifier>();

        public event Action OnModifiersChanged;

        public IReadOnlyList<EquipmentModifier> Active => _modifiers;

        /// <summary>Add modifier. Nếu cùng Source đã có → replace (vd: equip lại cùng item).</summary>
        public void Add(EquipmentModifier mod)
        {
            _modifiers.RemoveAll(m => m.Source == mod.Source);
            _modifiers.Add(mod);
            OnModifiersChanged?.Invoke();
        }

        /// <summary>Remove tất cả modifier có Source này (vd: unequip 1 item có 3 modifier).</summary>
        public bool Remove(string source)
        {
            int removed = _modifiers.RemoveAll(m => m.Source == source);
            if (removed > 0) OnModifiersChanged?.Invoke();
            return removed > 0;
        }

        /// <summary>Tính tổng modifier cho 1 stat type.</summary>
        public ModifierTotal Calculate(StatType type)
        {
            var result = new ModifierTotal();
            foreach (var m in _modifiers)
            {
                if (m.Type != type) continue;
                if (m.Kind == ModifierKind.Flat) result.Flat += m.Value;
                else result.Percent += m.Value;
            }
            return result;
        }

        public void Clear()
        {
            if (_modifiers.Count == 0) return;
            _modifiers.Clear();
            OnModifiersChanged?.Invoke();
        }
    }

    /// <summary>
    /// Tổng modifier cho 1 stat - apply lên base value để ra final.
    /// Formula: final = (base + flat) * (1 + percent/100)
    /// </summary>
    public struct ModifierTotal
    {
        public float Flat;
        public float Percent;  // 15 = +15%

        public int Apply(int baseValue)
            => Mathf.RoundToInt((baseValue + Flat) * (1f + Percent / 100f));

        public float ApplyFloat(float baseValue)
            => (baseValue + Flat) * (1f + Percent / 100f);
    }
}
