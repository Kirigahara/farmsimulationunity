using UnityEditor;
using UnityEditor.UI;
using GameTemplate.Core.UI.Buttons;

namespace GameTemplate.Editor.UI
{
    /// <summary>
    /// Custom Inspector cho HoldButton.
    /// HoldButton kế thừa Selectable, dùng SelectableEditor để vẽ field gốc.
    /// </summary>
    [CustomEditor(typeof(HoldButton))]
    [CanEditMultipleObjects]
    public class HoldButtonEditor : SelectableEditor
    {
        private SerializedProperty _onStartAction;
        private SerializedProperty _onHoldAction;
        private SerializedProperty _onReleaseAction;
        private SerializedProperty _releaseOnPointerExit;
        private SerializedProperty _hapticOnStart;
        private SerializedProperty _hapticOnRelease;

        protected override void OnEnable()
        {
            base.OnEnable();

            _onStartAction = serializedObject.FindProperty("_onStartAction");
            _onHoldAction = serializedObject.FindProperty("_onHoldAction");
            _onReleaseAction = serializedObject.FindProperty("_onReleaseAction");
            _releaseOnPointerExit = serializedObject.FindProperty("_releaseOnPointerExit");
            _hapticOnStart = serializedObject.FindProperty("_hapticOnStart");
            _hapticOnRelease = serializedObject.FindProperty("_hapticOnRelease");
        }

        public override void OnInspectorGUI()
        {
            // 1. Vẽ field gốc của Selectable (Interactable, Transition, Navigation)
            base.OnInspectorGUI();

            // 2. Vẽ field mới của HoldButton
            EditorGUILayout.Space(10);
            serializedObject.Update();

            // ===== Events (3 UnityEvent) =====
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_onStartAction);
            EditorGUILayout.PropertyField(_onHoldAction);
            EditorGUILayout.PropertyField(_onReleaseAction);

            EditorGUILayout.Space(5);

            // ===== Settings =====
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_releaseOnPointerExit);

            EditorGUILayout.Space(5);

            // ===== Haptic =====
            EditorGUILayout.LabelField("Haptic (optional)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_hapticOnStart);
            EditorGUILayout.PropertyField(_hapticOnRelease);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
