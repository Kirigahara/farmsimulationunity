using UnityEditor;
using UnityEngine;
using GameTemplate.Core.Patterns.Attributes;

namespace GameTemplate.Editor.MetaTools
{
    // ====================================================================
    // PropertyDrawer cho các attribute trong InspectorAttributes.cs
    // Chỉ compile khi build cho Editor -> không ảnh hưởng size build production.
    // ====================================================================

    [CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
    public class MinMaxRangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector2)
            {
                EditorGUI.LabelField(position, label.text, "Chỉ dùng cho Vector2");
                return;
            }

            var attr = (MinMaxRangeAttribute)attribute;
            var v = property.vector2Value;

            // Layout: Label | Min field | Slider | Max field
            const float labelWidth = 100f;
            const float fieldWidth = 50f;
            const float padding = 5f;

            var labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            var minRect = new Rect(labelRect.xMax + padding, position.y, fieldWidth, position.height);
            var sliderRect = new Rect(minRect.xMax + padding, position.y,
                position.width - labelWidth - fieldWidth * 2 - padding * 4, position.height);
            var maxRect = new Rect(sliderRect.xMax + padding, position.y, fieldWidth, position.height);

            EditorGUI.LabelField(labelRect, label);
            v.x = EditorGUI.FloatField(minRect, v.x);
            EditorGUI.MinMaxSlider(sliderRect, ref v.x, ref v.y, attr.Min, attr.Max);
            v.y = EditorGUI.FloatField(maxRect, v.y);

            // Clamp
            v.x = Mathf.Clamp(v.x, attr.Min, v.y);
            v.y = Mathf.Clamp(v.y, v.x, attr.Max);
            property.vector2Value = v;
        }
    }

    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
                EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!ShouldShow(property)) return 0f;
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private bool ShouldShow(SerializedProperty property)
        {
            var attr = (ShowIfAttribute)attribute;
            var path = property.propertyPath;
            var idx = path.LastIndexOf('.');
            var conditionPath = idx >= 0
                ? path.Substring(0, idx + 1) + attr.ConditionField
                : attr.ConditionField;

            var conditionProp = property.serializedObject.FindProperty(conditionPath);
            if (conditionProp == null || conditionProp.propertyType != SerializedPropertyType.Boolean)
                return true;

            return conditionProp.boolValue == attr.ExpectedValue;
        }
    }

    [CustomPropertyDrawer(typeof(TagAttribute))]
    public class TagDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
        }
    }

    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public class LayerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        }
    }

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }

    [CustomPropertyDrawer(typeof(InspectorNoteAttribute))]
    public class InspectorNoteDrawer : DecoratorDrawer
    {
        public override void OnGUI(Rect position)
        {
            var attr = (InspectorNoteAttribute)attribute;
            var msgType = (MessageType)(int)attr.Type;
            EditorGUI.HelpBox(position, attr.Note, msgType);
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight * 2f;
        }
    }
}
