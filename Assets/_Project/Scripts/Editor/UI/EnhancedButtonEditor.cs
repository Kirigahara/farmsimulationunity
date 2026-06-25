using UnityEditor;
using UnityEditor.UI;
using GameTemplate.Core.UI.Buttons;

namespace GameTemplate.Editor.UI
{
    /// <summary>
    /// Custom Inspector cho EnhancedButton.
    ///
    /// Lý do tồn tại: Unity dùng `ButtonEditor` cho Button component, editor này chỉ vẽ field của Button.
    /// Class kế thừa Button (EnhancedButton) thêm field mới → Inspector mặc định KHÔNG hiện.
    ///
    /// Fix: kế thừa ButtonEditor, gọi base.OnInspectorGUI() để vẽ field Button gốc,
    /// rồi vẽ thêm field của EnhancedButton bên dưới.
    /// </summary>
    [CustomEditor(typeof(EnhancedButton))]
    [CanEditMultipleObjects]
    public class EnhancedButtonEditor : ButtonEditor
    {
        // SerializedProperty cho các field thêm của EnhancedButton
        private SerializedProperty _sfxPreset;
        private SerializedProperty _customSfx;
        private SerializedProperty _sfxVolumeScale;
        private SerializedProperty _trackEventName;
        private SerializedProperty _hapticType;
        private SerializedProperty _minIntervalBetweenClicks;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Lookup field theo tên - phải khớp chính xác tên private field trong EnhancedButton.cs
            _sfxPreset = serializedObject.FindProperty("_sfxPreset");
            _customSfx = serializedObject.FindProperty("_customSfx");
            _sfxVolumeScale = serializedObject.FindProperty("_sfxVolumeScale");
            _trackEventName = serializedObject.FindProperty("_trackEventName");
            _hapticType = serializedObject.FindProperty("_hapticType");
            _minIntervalBetweenClicks = serializedObject.FindProperty("_minIntervalBetweenClicks");
        }

        public override void OnInspectorGUI()
        {
            // 1. Vẽ field gốc của Button (Interactable, Transition, Navigation, OnClick)
            base.OnInspectorGUI();

            // 2. Vẽ field mới của EnhancedButton
            EditorGUILayout.Space(10);
            serializedObject.Update();

            // ===== Sound Effect =====
            EditorGUILayout.LabelField("Sound Effect", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_sfxPreset);
            EditorGUILayout.PropertyField(_customSfx);
            EditorGUILayout.PropertyField(_sfxVolumeScale);

            EditorGUILayout.Space(5);

            // ===== Analytics Tracking =====
            EditorGUILayout.LabelField("Analytics Tracking", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_trackEventName);

            EditorGUILayout.Space(5);

            // ===== Haptic =====
            EditorGUILayout.LabelField("Haptic Feedback (Mobile)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_hapticType);

            EditorGUILayout.Space(5);

            // ===== Spam Protection =====
            EditorGUILayout.LabelField("Spam Protection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_minIntervalBetweenClicks);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
