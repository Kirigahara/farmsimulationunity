# Editor Tools

Template cung cấp **8 universal editor tools** + **meta-tools** để team build game-specific editors nhanh hơn.

## Đảm bảo không ảnh hưởng dung lượng build

Unity tự strip editor code khỏi build bằng 3 cơ chế:

| Cơ chế | Áp dụng cho | Cách nhận biết |
|---|---|---|
| Folder `Editor/` | Mọi script trong folder tên `Editor` | Folder `Assets/_Project/Scripts/Editor/` |
| Assembly Definition `includePlatforms: ["Editor"]` | Cả assembly compile chỉ cho Editor | File `GameTemplate.Editor.asmdef` |
| `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` | Code runtime cần debug trên device | CheatConsole, SaveInspector |

**Verify trước khi ship release:**

```
File > Build Settings > Development Build = OFF
Build Android Release qua menu GameTemplate > Build > Android Release
```

Build production sẽ KHÔNG chứa:
- Mọi code trong `Scripts/Editor/`
- CheatConsole, SaveInspector (vì `DEVELOPMENT_BUILD` chưa define)

## 8 Tools có sẵn

### Nhóm 1: Infrastructure

#### 1.1 Scene Auto-Loader
- **Menu:** `GameTemplate > Scene Auto-Loader > Enabled`
- **Tính năng:** Bấm Play ở scene bất kỳ → tự load Bootstrap trước → Stop về scene cũ.
- **Extend:** Đổi `BootstrapScenePath` trong code nếu folder scene khác.

#### 1.2 Define Symbol Manager
- **Menu:** `GameTemplate > Define Symbol Manager`
- **Tính năng:** UI checkbox để bật/tắt define (`ADS_UNITY`, `ENABLE_GAME_LOG`...).
- **Extend:** Thêm define mới vào `_groups` array trong code:
  ```csharp
  new DefineGroup("Multiplayer", new[]
  {
      new DefineEntry("MULTIPLAYER_PHOTON", "Photon SDK"),
      new DefineEntry("MULTIPLAYER_MIRROR", "Mirror Networking"),
  }),
  ```

#### 1.3 Build Pipeline
- **Menu:** `GameTemplate > Build > Android/iOS Dev/Release`
- **Tính năng:** One-click build, auto increment version, output theo timestamp folder.
- **Extend:**
  - Thêm webhook Slack: trong `ExecuteBuild`, sau khi `BuildResult.Succeeded` gọi API Slack.
  - Auto upload Firebase Distribution: dùng `firebase appdistribution:distribute` qua `System.Diagnostics.Process`.

### Nhóm 2: Quality of Life

#### 2.1 Hierarchy Icons
- **Tự động:** Hiện icon component bên cạnh GameObject trong Hierarchy.
- **Extend:** Thêm icon cho component custom (vd PlayerController) trong `_iconMappings`:
  ```csharp
  (typeof(PlayerController), "Player"),
  (typeof(EnemyAI), "Enemy"),
  ```

#### 2.2 Project Shortcuts
- **Menu:** `GameTemplate > Open/Folder/Clear`
- **Tính năng:** Quick open Bootstrap scene, Build settings, Persistent Data folder, clear PlayerPrefs/saves.
- **Extend:** Thêm shortcut riêng cho game:
  ```csharp
  [MenuItem("GameTemplate/Open/Level Editor", priority = 35)]
  public static void OpenLevelEditor() => LevelEditorWindow.Open();
  ```

#### 2.3 ScriptableObject Browser
- **Menu:** `GameTemplate > ScriptableObject Browser`
- **Tính năng:** Window xem mọi SO trong project, lọc theo type, edit inline.
- **Use case:** RPG có 50 enemy SO → mở browser duyệt balance nhanh, không phải tìm từng file.
- **Tự động:** Detect mọi class kế thừa ScriptableObject. Không cần config gì.

### Nhóm 3: Asset Workflow

#### 3.1 CSV → Localization Importer
- **Menu:** `GameTemplate > Import > CSV to Localization Table`
- **Workflow chuẩn:**
  1. Designer làm sheet Google Sheets với cột `Key | English | Vietnamese | Japanese...`
  2. File → Download → CSV
  3. Unity: GameTemplate > Import > CSV to Localization Table
  4. Chọn file CSV vừa download, chọn nơi save `.asset`
  5. Game tự dùng table mới qua `ServiceLocator.Get<ILocalizationService>()`
- **Format CSV:**
  ```
  Key,English,Vietnamese,Japanese
  ui.play,Play,Chơi,プレイ
  ui.settings,Settings,Cài đặt,設定
  ```
- **Extend:** Tạo importer tương tự cho ItemData, EnemyData... copy file `CsvToLocalizationImporter.cs` làm template.

### Nhóm 4: Meta-Tools (để build game-specific tools)

#### 4.1 Inspector Attributes + PropertyDrawers
Attributes có sẵn dùng được ngay trong ScriptableObject hoặc MonoBehaviour:

```csharp
public class EnemyData : ScriptableObject
{
    [MinMaxRange(0, 100)] public Vector2 DamageRange;
    [Tag] public string TargetTag;
    [Layer] public int EnemyLayer;
    [ReadOnly] public string Id; // không cho sửa trong inspector
    [InspectorNote("Boss chỉ spawn 1 lần per level", InspectorNoteAttribute.MessageType.Warning)]
    public bool IsBoss;
    [ShowIf(nameof(IsBoss))] public BossConfig BossSettings;
}
```

**Extend - thêm attribute mới:**
1. Tạo `MyAttribute : PropertyAttribute` trong `Scripts/Core/Patterns/Attributes/`
2. Tạo `MyDrawer : PropertyDrawer` trong `Scripts/Editor/MetaTools/` với `[CustomPropertyDrawer(typeof(MyAttribute))]`

#### 4.2 EditorWindowBase<T>
Base class để build custom editor cho data game-specific. Có sẵn:
- Toolbar (New, Refresh, Search)
- List panel với search filter
- Detail panel với default inspector
- Delete confirmation

**Tạo Quest Editor cho game RPG trong 30 dòng:**

```csharp
using UnityEditor;
using UnityEngine;
using GameTemplate.Editor.MetaTools;

public class QuestEditorWindow : EditorWindowBase<Quest>
{
    [MenuItem("MyGame/Quest Editor")]
    public static void Open() => GetWindow<QuestEditorWindow>("Quest Editor");

    protected override Quest CreateNew()
    {
        var quest = CreateInstance<Quest>();
        var path = EditorUtility.SaveFilePanelInProject(
            "New Quest", "NewQuest", "asset", "");
        if (string.IsNullOrEmpty(path)) return null;
        AssetDatabase.CreateAsset(quest, path);
        return quest;
    }

    protected override string GetItemDisplayName(Quest q) => q.QuestName;

    protected override void DrawCustomToolbar()
    {
        if (GUILayout.Button("Validate All", EditorStyles.toolbarButton))
        {
            foreach (var q in _items) q.Validate();
        }
    }
}
```

Có ngay UI 3-panel chuyên nghiệp, không phải code lại từ đầu.

### Nhóm 5: Debug Runtime (Cheat Console + Save Inspector)

#### 5.1 Cheat Console
- **Toggle:** `Tab` (Editor), 3-finger tap (mobile dev build)
- **Built-in commands:** `help`, `clear`, `close`, `fps`, `scene`, `time_scale`
- **Extend - cách 1 (RegisterCommand):**
  ```csharp
  void Start()
  {
      CheatConsole.RegisterCommand("add_coin", "add_coin <amount>", args =>
      {
          int amount = int.Parse(args[0]);
          PlayerData.Instance.Coins += amount;
      });
  }
  ```
- **Extend - cách 2 (attribute, recommend):**
  ```csharp
  public static class GameplayCheats
  {
      [CheatCommand("god_mode", "Toggle god mode")]
      static void GodMode(string[] args)
      {
          Player.IsGod = !Player.IsGod;
      }

      [CheatCommand("set_level", "set_level <index>")]
      static void SetLevel(string[] args)
      {
          int idx = int.Parse(args[0]);
          GameManager.LoadLevel(idx);
      }
  }
  ```
  Console tự discover qua reflection, không cần đăng ký tay.

#### 5.2 Save Inspector
- **Toggle:** `F2` (Editor), 4-finger tap (mobile dev build)
- **Tính năng:** Xem file save JSON, edit trực tiếp, lưu lại, delete file.
- **Use case:** Test edge case "hp = 0", "coin overflow", "level 999" mà không phải replay game.

#### Đảm bảo release build sạch
Build menu `Build > Android Release` đã tắt `Development Build` mặc định. Verify:

```csharp
// Code này trong gameplay vẫn compile được dù release build:
CheatConsole.RegisterCommand("test", "", args => { });
// Vì stub class luôn tồn tại. Method chỉ là no-op trong release.
```

Quan trọng: **KHÔNG để key dev** (Tab, F2, multi-touch) trigger logic gì khác. Người dùng cuối nhấn vào không có gì xảy ra (Console không tồn tại trong release).

## Best practices cho team

1. **Đừng commit Development Build option đang ON.** Default OFF, dev tự bật khi cần.
2. **Thêm cheat khi gặp bug khó tái hiện.** Tăng năng suất QA.
3. **Cheat command nên có prefix theo system:** `audio_volume`, `audio_play`, `player_god`, `player_coin`. Dễ tra cứu.
4. **Editor tool extend dần khi cần.** Đừng build sẵn tool cho feature chưa có.

## Bảng tổng kết tools

| Tool | Folder | Strip khỏi build? |
|---|---|---|
| Scene Auto-Loader | `Editor/Infrastructure/` | ✅ (folder Editor) |
| Define Symbol Manager | `Editor/Infrastructure/` | ✅ |
| Build Pipeline | `Editor/Infrastructure/` | ✅ |
| Hierarchy Icons | `Editor/QualityOfLife/` | ✅ |
| Project Shortcuts | `Editor/QualityOfLife/` | ✅ |
| SO Browser | `Editor/QualityOfLife/` | ✅ |
| CSV Importer | `Editor/AssetWorkflow/` | ✅ |
| Inspector PropertyDrawers | `Editor/MetaTools/` | ✅ |
| EditorWindowBase | `Editor/MetaTools/` | ✅ |
| **Cheat Console** | `Core/DevTools/` | ⚠️ Chỉ trong `DEVELOPMENT_BUILD` |
| **Save Inspector** | `Core/DevTools/` | ⚠️ Chỉ trong `DEVELOPMENT_BUILD` |
| Inspector Attributes | `Core/Patterns/Attributes/` | ❌ (luôn có, vì PropertyAttribute là base Unity, attributes runtime chỉ ~1KB) |
