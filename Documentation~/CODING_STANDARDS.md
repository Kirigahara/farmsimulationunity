# Coding Standards

Quy tắc code chung. Bám sát để PR review nhanh, code đồng nhất.

## Naming

| Loại | Convention | Ví dụ |
|---|---|---|
| Class, struct, interface | PascalCase | `PlayerController`, `IAudioService` |
| Interface | Bắt đầu `I` | `ISaveService` |
| Method, property | PascalCase | `PlaySfx()`, `IsAlive` |
| Public field | PascalCase | `public int Score;` (hạn chế dùng, prefer property) |
| Private field | `_camelCase` | `private int _hp;` |
| Local var, parameter | camelCase | `var damage = 10;` |
| Constant | PascalCase | `const int MaxLevel = 100;` |
| Event | Past tense | `PlayerDiedEvent`, `LevelLoadedEvent` |

## Folder rules

| Code | Đặt ở | Namespace |
|---|---|---|
| Framework dùng nhiều game | `Scripts/Core/<system>/` | `GameTemplate.Core.<System>` |
| Logic game này | `Scripts/Gameplay/<feature>/` | `GameTemplate.Gameplay.<Feature>` |
| ScriptableObject data | `Scripts/Data/` | `GameTemplate.Data` |
| UI panel | `Scripts/UI/` | `GameTemplate.UI` |
| Editor tools | `Scripts/Editor/` | `GameTemplate.Editor` |

**Quy tắc vàng:** Code Core KHÔNG `using GameTemplate.Gameplay`. Nếu cần → có gì đó sai, refactor.

### ⚠️ Quy tắc tên file = tên class (cho MonoBehaviour & ScriptableObject)

Unity yêu cầu file `.cs` chứa MonoBehaviour hoặc ScriptableObject phải **tên file khớp tên class**, nếu không Add Component / CreateAssetMenu sẽ không tìm thấy:

| Loại class | Tên file bắt buộc? | Lý do |
|---|---|---|
| MonoBehaviour | ✅ Bắt buộc | Unity Add Component search theo tên file |
| ScriptableObject | ✅ Bắt buộc | Unity load asset theo tên type |
| Pure C# class | ❌ Không bắt buộc | Nhưng nên giữ cho rõ |
| Interface, struct, enum | ❌ Không bắt buộc | Có thể gộp nhiều cái trong 1 file |

**Lỗi điển hình:** 1 file chứa 2 MonoBehaviour → chỉ class trùng tên file mới search được. Tách thành 2 file riêng.

Ví dụ sai: `UIPanel.cs` chứa cả `class UIPanel` và `class UIManager` → Add Component không thấy `UIManager`.
Ví dụ đúng: 2 file `UIPanel.cs` và `UIManager.cs` riêng.

### ⚠️ Cẩn thận khi đặt tên namespace

**KHÔNG dùng các tên trùng với class/enum Unity built-in** cho namespace HOẶC type:

| ❌ Tránh | ✅ Dùng thay thế | Lý do |
|---|---|---|
| namespace `GameTemplate.Core.Debug` | `GameTemplate.Core.DevTools` | Conflict với `UnityEngine.Debug` |
| namespace `GameTemplate.Core.Input` | `GameTemplate.Core.InputSystem` | Conflict với `UnityEngine.Input` |
| namespace `GameTemplate.Core.Random` | `GameTemplate.Core.Rng` | Conflict với `UnityEngine.Random` |
| namespace `GameTemplate.Core.Object` | `GameTemplate.Core.Objects` | Conflict với `UnityEngine.Object` |
| namespace `GameTemplate.Core.Application` | `GameTemplate.Core.App` | Conflict với `UnityEngine.Application` |
| enum `SystemLanguage` | `GameLanguage` | Conflict với `UnityEngine.SystemLanguage` |
| enum `Color` | `GameColor` / `PaletteColor` | Conflict với `UnityEngine.Color` |
| class `Camera` | `GameCamera` / `CameraController` | Conflict với `UnityEngine.Camera` |

**Triệu chứng nếu bị conflict:**
- Namespace conflict: error CS0234 "name 'LogXxx' does not exist in namespace ..."
- Type conflict: error CS0104 "'SystemLanguage' is an ambiguous reference between..."

**Quy tắc nhanh:** Trước khi đặt tên type/namespace, search "UnityEngine.<TênBạnĐịnhĐặt>" trên docs.unity3d.com. Nếu Unity đã có → đổi tên.

## Performance mobile - DO / DON'T

### DO

- ✅ Pool object spawn/despawn liên tục (bullet, enemy, VFX)
- ✅ Cache reference trong Awake: `_rb = GetComponent<Rigidbody>();`
- ✅ Dùng `struct` cho game event (zero alloc)
- ✅ `TryGetComponent` thay vì `GetComponent` + null check
- ✅ String formatting với `$"..."` nhưng tránh trong vòng lặp Update
- ✅ Tắt log production: bỏ `ENABLE_GAME_LOG` define

### DON'T

- ❌ `GameObject.Find` / `FindObjectOfType` trong Update (chậm)
- ❌ `FindObjectOfType` (Unity 2023+) → dùng `FindAnyObjectByType` (nhanh hơn) hoặc `FindFirstObjectByType`
- ❌ `Instantiate` / `Destroy` trong vòng lặp → dùng pool
- ❌ `Camera.main` trong Update → cache 1 lần
- ❌ `Resources.Load` runtime → preload trong Bootstrap hoặc dùng Addressables
- ❌ `new` cho class trong Update (alloc → GC spike)
- ❌ LINQ trong hot path (vd Update, FixedUpdate)
- ❌ String concatenation trong Update (`"Score: " + score`) → dùng `StringBuilder` hoặc UI cached

## Event Bus quy tắc

```csharp
// ✅ ĐÚNG
private void OnEnable()  => EventBus.Subscribe<MyEvent>(OnEvent);
private void OnDisable() => EventBus.Unsubscribe<MyEvent>(OnEvent);

// ❌ SAI - subscribe nhưng quên unsubscribe -> memory leak
private void Start() { EventBus.Subscribe<MyEvent>(OnEvent); }

// ❌ SAI - subscribe trong Update -> sub nhiều lần
private void Update() { EventBus.Subscribe<MyEvent>(OnEvent); }
```

## ScriptableObject cho data

Mọi data tĩnh (level config, enemy stat, sound database) → ScriptableObject, không hardcode trong script:

```csharp
[CreateAssetMenu(menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public int MaxHp;
    public float Speed;
    public AudioClip DeathSfx;
}
```

Lợi ích: designer chỉnh được trong Inspector, không cần coder.

## Serializing

```csharp
// ✅ Dùng SerializeField cho private, không expose public
[SerializeField] private float _moveSpeed = 5f;

// ❌ public field - bị access từ mọi nơi, vi phạm encapsulation
public float MoveSpeed = 5f;
```

## Khi review PR, kiểm tra

- [ ] Có Subscribe/Unsubscribe đầy đủ?
- [ ] Có FindObjectOfType, GameObject.Find trong runtime không?
- [ ] Có alloc trong Update không (new, string concat, LINQ)?
- [ ] Naming convention?
- [ ] Folder đúng (Core vs Gameplay)?
- [ ] Comment XML cho public API của Core?
- [ ] Test bằng tay trên device thật (không chỉ Editor)?
