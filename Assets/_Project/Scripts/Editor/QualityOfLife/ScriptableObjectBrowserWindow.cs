using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameTemplate.Editor.QualityOfLife
{
    /// <summary>
    /// ScriptableObject Browser - xem mọi SO trong project ở 1 cửa sổ.
    /// Lọc theo type, search tên, edit ngay trong panel.
    ///
    /// Use case:
    ///   - RPG có 50 enemy SO, cần xem tổng quan stats
    ///   - Puzzle có 100 level SO, muốn duyệt nhanh
    ///   - Designer balance, không phải tìm từng file
    /// </summary>
    public class ScriptableObjectBrowserWindow : EditorWindow
    {
        private System.Type _selectedType;
        private List<System.Type> _availableTypes;
        private Vector2 _typeListScroll;
        private Vector2 _objectListScroll;
        private Vector2 _inspectorScroll;
        private string _searchFilter = "";
        private List<ScriptableObject> _objects = new List<ScriptableObject>();
        private ScriptableObject _selectedObject;
        private UnityEditor.Editor _embeddedEditor;

        [MenuItem("GameTemplate/ScriptableObject Browser", priority = 40)]
        public static void Open()
        {
            var window = GetWindow<ScriptableObjectBrowserWindow>("SO Browser");
            window.minSize = new Vector2(800, 500);
        }

        private void OnEnable()
        {
            RefreshTypes();
        }

        private void RefreshTypes()
        {
            // Lấy mọi type kế thừa ScriptableObject trong project (không phải Unity built-in)
            _availableTypes = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                .Where(t => !t.IsAbstract
                            && !t.FullName.StartsWith("UnityEngine.")
                            && !t.FullName.StartsWith("UnityEditor."))
                .OrderBy(t => t.Name)
                .ToList();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // ============ LEFT: Type list ============
            EditorGUILayout.BeginVertical(GUILayout.Width(220));
            EditorGUILayout.LabelField("Types", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh Types", GUILayout.Height(20)))
                RefreshTypes();

            _typeListScroll = EditorGUILayout.BeginScrollView(_typeListScroll);
            foreach (var type in _availableTypes)
            {
                if (GUILayout.Toggle(_selectedType == type, type.Name, "Button"))
                {
                    if (_selectedType != type)
                    {
                        _selectedType = type;
                        RefreshObjects();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // ============ MIDDLE: Object list ============
            EditorGUILayout.BeginVertical(GUILayout.Width(280));
            EditorGUILayout.LabelField(
                _selectedType != null ? $"{_selectedType.Name} ({_objects.Count})" : "Objects",
                EditorStyles.boldLabel);

            _searchFilter = EditorGUILayout.TextField("Search", _searchFilter);

            _objectListScroll = EditorGUILayout.BeginScrollView(_objectListScroll);
            if (_objects != null)
            {
                foreach (var obj in _objects)
                {
                    if (obj == null) continue;
                    if (!string.IsNullOrEmpty(_searchFilter)
                        && !obj.name.ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant()))
                        continue;

                    var isSelected = _selectedObject == obj;
                    if (GUILayout.Toggle(isSelected, obj.name, "Button"))
                    {
                        if (!isSelected) SelectObject(obj);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // ============ RIGHT: Inspector ============
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(
                _selectedObject != null ? _selectedObject.name : "Inspector",
                EditorStyles.boldLabel);

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
            if (_embeddedEditor != null)
            {
                _embeddedEditor.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.HelpBox("Chọn type và object để xem detail.", MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshObjects()
        {
            _objects.Clear();
            _selectedObject = null;
            if (_embeddedEditor != null)
            {
                DestroyImmediate(_embeddedEditor);
                _embeddedEditor = null;
            }
            if (_selectedType == null) return;

            var guids = AssetDatabase.FindAssets($"t:{_selectedType.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath(path, _selectedType) as ScriptableObject;
                if (obj != null) _objects.Add(obj);
            }
            _objects.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        }

        private void SelectObject(ScriptableObject obj)
        {
            _selectedObject = obj;
            if (_embeddedEditor != null) DestroyImmediate(_embeddedEditor);
            _embeddedEditor = UnityEditor.Editor.CreateEditor(obj);
        }
    }
}
