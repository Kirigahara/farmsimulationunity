using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameTemplate.Editor.MetaTools
{
    /// <summary>
    /// Base class cho editor window 3-panel: Toolbar | List | Detail.
    /// Dùng để build custom editor cho data game-specific nhanh.
    ///
    /// Vd: Quest Editor, Skill Editor, Level Editor cho game RPG.
    ///
    /// Cách dùng:
    ///   public class QuestEditorWindow : EditorWindowBase<Quest>
    ///   {
    ///       [MenuItem("MyGame/Quest Editor")]
    ///       static void Open() => GetWindow<QuestEditorWindow>("Quests");
    ///
    ///       protected override Quest CreateNew()
    ///       {
    ///           var q = CreateInstance<Quest>();
    ///           AssetDatabase.CreateAsset(q, "Assets/Data/Quests/NewQuest.asset");
    ///           return q;
    ///       }
    ///
    ///       protected override string GetItemDisplayName(Quest item) => item.QuestName;
    ///   }
    /// </summary>
    public abstract class EditorWindowBase<T> : EditorWindow where T : ScriptableObject
    {
        protected List<T> _items = new List<T>();
        protected T _selectedItem;
        protected UnityEditor.Editor _selectedEditor;
        protected string _searchFilter = "";

        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private const float ListWidth = 260f;

        protected virtual void OnEnable() => RefreshItems();

        /// <summary>Override để custom cách tạo item mới (vd: tạo .asset file).</summary>
        protected abstract T CreateNew();

        /// <summary>Override để hiển thị tên item trong list (vd: dùng field "QuestName").</summary>
        protected virtual string GetItemDisplayName(T item) => item.name;

        /// <summary>Override để custom toolbar buttons.</summary>
        protected virtual void DrawCustomToolbar() { }

        /// <summary>Override để custom inspector panel bên phải (default: dùng default inspector).</summary>
        protected virtual void DrawDetailPanel(T item)
        {
            if (_selectedEditor != null) _selectedEditor.OnInspectorGUI();
        }

        protected void RefreshItems()
        {
            _items.Clear();
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<T>(path);
                if (item != null) _items.Add(item);
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawListPanel();
            EditorGUILayout.BeginVertical();
            DrawDetailHeader();
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            if (_selectedItem != null) DrawDetailPanel(_selectedItem);
            else EditorGUILayout.HelpBox("Chọn 1 item từ list bên trái, hoặc bấm '+' để tạo mới.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("+ New", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                var newItem = CreateNew();
                if (newItem != null)
                {
                    RefreshItems();
                    Select(newItem);
                }
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                RefreshItems();

            GUILayout.FlexibleSpace();
            DrawCustomToolbar();
            GUILayout.FlexibleSpace();

            GUILayout.Label("Search:", GUILayout.Width(50));
            _searchFilter = GUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth));
            EditorGUILayout.LabelField($"Items ({_items.Count})", EditorStyles.boldLabel);

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            foreach (var item in _items)
            {
                if (item == null) continue;
                var name = GetItemDisplayName(item);
                if (!string.IsNullOrEmpty(_searchFilter)
                    && !name.ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant()))
                    continue;

                bool isSelected = _selectedItem == item;
                if (GUILayout.Toggle(isSelected, name, "Button"))
                {
                    if (!isSelected) Select(item);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDetailHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                _selectedItem != null ? GetItemDisplayName(_selectedItem) : "Detail",
                EditorStyles.boldLabel);

            if (_selectedItem != null)
            {
                if (GUILayout.Button("Ping", GUILayout.Width(60)))
                    EditorGUIUtility.PingObject(_selectedItem);
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    DeleteSelected();
            }
            EditorGUILayout.EndHorizontal();
        }

        protected void Select(T item)
        {
            _selectedItem = item;
            if (_selectedEditor != null) DestroyImmediate(_selectedEditor);
            _selectedEditor = UnityEditor.Editor.CreateEditor(item);
        }

        private void DeleteSelected()
        {
            if (_selectedItem == null) return;
            if (!EditorUtility.DisplayDialog("Delete",
                $"Xóa '{GetItemDisplayName(_selectedItem)}'?", "Delete", "Cancel"))
                return;

            var path = AssetDatabase.GetAssetPath(_selectedItem);
            AssetDatabase.DeleteAsset(path);
            _selectedItem = null;
            if (_selectedEditor != null) DestroyImmediate(_selectedEditor);
            RefreshItems();
        }
    }
}
