using UnityEngine;

namespace GameTemplate.Core.Patterns.Attributes
{
    // ====================================================================
    // Attributes này nằm trong Core (không phải Editor) vì cần dùng ở Inspector
    // runtime cũng có thể đọc. PropertyDrawer tương ứng nằm trong Editor.
    // ====================================================================

    /// <summary>
    /// Hiện slider min-max cho Vector2 (vd: damage range, spawn count range).
    /// Cách dùng: [MinMaxRange(0, 100)] public Vector2 DamageRange;
    /// </summary>
    public class MinMaxRangeAttribute : PropertyAttribute
    {
        public float Min;
        public float Max;
        public MinMaxRangeAttribute(float min, float max) { Min = min; Max = max; }
    }

    /// <summary>
    /// Chỉ hiện field này khi điều kiện thỏa.
    /// Cách dùng: [ShowIf(nameof(IsBoss))] public BossData Config;
    /// </summary>
    public class ShowIfAttribute : PropertyAttribute
    {
        public string ConditionField;
        public bool ExpectedValue;
        public ShowIfAttribute(string conditionField, bool expectedValue = true)
        {
            ConditionField = conditionField;
            ExpectedValue = expectedValue;
        }
    }

    /// <summary>
    /// Dropdown chọn Tag thay vì gõ tay string.
    /// Cách dùng: [Tag] public string TargetTag;
    /// </summary>
    public class TagAttribute : PropertyAttribute { }

    /// <summary>
    /// Dropdown chọn Layer thay vì gõ tay int.
    /// Cách dùng: [Layer] public int EnemyLayer;
    /// </summary>
    public class LayerAttribute : PropertyAttribute { }

    /// <summary>
    /// Hiển thị field read-only trong inspector (vd: debug info).
    /// Cách dùng: [ReadOnly] public float CurrentSpeed;
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }

    /// <summary>
    /// Note/help text hiển thị phía trên field như Help Box.
    /// Cách dùng: [InspectorNote("Đừng để âm")] public int Damage;
    /// </summary>
    public class InspectorNoteAttribute : PropertyAttribute
    {
        public string Note;
        public MessageType Type;
        public enum MessageType { Info, Warning, Error }
        public InspectorNoteAttribute(string note, MessageType type = MessageType.Info)
        {
            Note = note;
            Type = type;
        }
    }
}
