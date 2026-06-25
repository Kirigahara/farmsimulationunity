using UnityEngine;

namespace GameTemplate.Gameplay.Stats
{
    /// <summary>
    /// EquipmentItem - 1 item có thể equip lên character.
    /// 
    /// Là ScriptableObject để designer chỉnh trong Editor.
    /// Mỗi item có 1+ EquipmentModifier (vd: "Iron Sword" có +20 Attack, +5% Crit).
    /// 
    /// Tạo asset: Right-click Project → Create → Game → Equipment Item
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Equipment Item", fileName = "Item_")]
    public class EquipmentItem : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _itemId = "iron_sword";
        [SerializeField] private string _displayName = "Iron Sword";
        [SerializeField] private Sprite _icon;
        [SerializeField] private EquipmentSlot _slot = EquipmentSlot.Weapon;

        [Header("Stats")]
        [SerializeField] private EquipmentModifier[] _modifiers;

        public string ItemId => _itemId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public EquipmentSlot Slot => _slot;
        public EquipmentModifier[] Modifiers => _modifiers;

        // Helper: tạo array modifier với Source = itemId, để pass vào CharacterStats.Equip
        public EquipmentModifier[] CreateRuntimeModifiers()
        {
            var result = new EquipmentModifier[_modifiers.Length];
            for (int i = 0; i < _modifiers.Length; i++)
            {
                result[i] = new EquipmentModifier
                {
                    Source = _itemId,  // dùng itemId làm source để dedupe + remove
                    Type = _modifiers[i].Type,
                    Kind = _modifiers[i].Kind,
                    Value = _modifiers[i].Value,
                };
            }
            return result;
        }
    }

    public enum EquipmentSlot
    {
        Weapon,
        Armor,
        Helmet,
        Boots,
        Accessory,
    }
}
