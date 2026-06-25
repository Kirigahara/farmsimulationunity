using UnityEditor;
using UnityEngine;

namespace GameTemplate.Editor.QualityOfLife
{
    /// <summary>
    /// Hierarchy Enhancer - hiện icon component chính bên cạnh GameObject trong Hierarchy.
    /// Tiện cho scene có 50+ GameObject - nhìn icon biết ngay đối tượng nào là Camera, Light, UI...
    ///
    /// Thêm icon mới: thêm 1 mục vào _iconMappings array.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyEnhancer
    {
        // (Loại Component, Tooltip hiển thị khi hover)
        private static readonly (System.Type type, string tooltip)[] _iconMappings = new[]
        {
            (typeof(Camera), "Camera"),
            (typeof(Light), "Light"),
            (typeof(Canvas), "UI Canvas"),
            (typeof(UnityEngine.UI.Button), "UI Button"),
            (typeof(UnityEngine.UI.Image), "UI Image"),
            (typeof(UnityEngine.UI.Text), "UI Text"),
            (typeof(AudioSource), "AudioSource"),
            (typeof(Rigidbody), "Rigidbody"),
            (typeof(Rigidbody2D), "Rigidbody2D"),
            (typeof(Collider), "Collider"),
            (typeof(Collider2D), "Collider2D"),
            (typeof(Animator), "Animator"),
            (typeof(ParticleSystem), "ParticleSystem"),
            (typeof(MeshRenderer), "MeshRenderer"),
            (typeof(SpriteRenderer), "SpriteRenderer"),
        };

        static HierarchyEnhancer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
        }

        private static void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            // Vẽ icon từ phải sang trái
            float x = selectionRect.xMax - 18f;
            const float iconSize = 14f;

            foreach (var mapping in _iconMappings)
            {
                if (go.GetComponent(mapping.type) == null) continue;

                var iconRect = new Rect(x, selectionRect.y + 2f, iconSize, iconSize);
                var content = EditorGUIUtility.ObjectContent(null, mapping.type);
                if (content.image != null)
                {
                    GUI.Label(iconRect, new GUIContent(content.image, mapping.tooltip));
                    x -= iconSize + 2f;
                }
            }
        }
    }
}
