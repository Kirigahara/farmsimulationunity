# API Reference

Tài liệu chi tiết mọi public class/method trong template. Tham khảo khi cần signature/example cụ thể.

**Quy ước:**
- 📁 = đường dẫn file source
- 📦 = namespace
- ⚠️ = lưu ý quan trọng
- 💡 = tip dùng

---

## Mục lục

**Core Framework:**
- [ServiceLocator](#servicelocator) - DI nhẹ
- [EventBus](#eventbus) - Pub/Sub
- [GameLog](#gamelog) - Logger có category
- [ObjectPool](#objectpool) - Pool generic
- [JsonSaveService](#jsonsaveservice) - Save/load
- [IDailyResetService](#idailyresetservice) - Check ngày mới, streak
- [TutorialSequencer](#tutorialsequencer) - Tutorial engine
- [ITutorialStep](#itutorialstep) - Tutorial step interface
- [ITutorialOverlay](#itutorialoverlay) - Tutorial UI overlay
- [AudioManager](#audiomanager) - Audio pool
- [SceneLoader](#sceneloader) - Async scene load
- [UIPanel + UIManager](#uipanel--uimanager) - UI stack
- [SafeAreaFitter](#safeareafitter) - Tránh notch/home indicator
- [CameraFitter](#camerafitter) - Fit gameplay area trên mọi device aspect
- [EnhancedButton](#enhancedbutton) - Button + SFX + Analytics + Haptic
- [HoldButton](#holdbutton) - Nút nhấn giữ
- [UIButtonSfxLibrary](#uibuttonsfxlibrary) - ScriptableObject map preset SFX

**Patterns:**
- [ReactiveProperty](#reactiveproperty) - Observable value
- [ReactiveCollection](#reactivecollection) - Observable list
- [PrefabFactory](#prefabfactory) - Spawn từ ScriptableObject
- [MVP base](#mvp-base) - Model/View/Presenter
- [MonoSingleton](#monosingleton) - Singleton an toàn
- [AsyncOp + Sequence](#asyncop--sequence) - Async helpers

**Mobile Services:**
- [IAdsService](#iadsservice) - Ads
- [IIapService](#iiapservice) - In-app purchase
- [IAnalyticsService](#ianalyticsservice) - Analytics
- [IRemoteConfigService](#iremoteconfigservice) - Remote config
- [ILocalizationService](#ilocalizationservice) - Localization
- [IHapticService](#ihapticservice) - Haptic
- [IDeviceInfoService](#ideviceinfoservice) - Device tier

**Dev Tools:**
- [CheatConsole](#cheatconsole) - Runtime cheat
- [SaveInspector](#saveinspector) - Save file editor
- [FpsDisplay](#fpsdisplay) - FPS counter overlay

---

## ServiceLocator

📁 `Assets/_Project/Scripts/Core/DI/ServiceLocator.cs`
📦 `GameTemplate.Core.DI`

DI nhẹ không phụ thuộc plugin. Dùng để các module tìm thấy service global mà không phải `FindObjectOfType`.

### Methods

| Method | Mô tả |
|---|---|
| `Register<T>(T service)` | Đăng ký service. Throw nếu service null. Override nếu type đã tồn tại. |
| `Get<T>()` | Lấy service. **Throw** nếu chưa register. |
| `TryGet<T>(out T service)` | An toàn hơn Get. Return false nếu chưa register. |
| `Unregister<T>()` | Xoá khỏi locator. |
| `Clear()` | Xoá tất cả. Dùng khi test hoặc shutdown. |

### Example

```csharp
// Register (chỉ ở Bootstrap)
ServiceLocator.Register<IAudioService>(audioManager);
ServiceLocator.Register<ISaveService>(new JsonSaveService());

// Use (mọi nơi khác)
var audio = ServiceLocator.Get<IAudioService>();
audio.PlaySfx(clip);

// Safe check
if (ServiceLocator.TryGet<IAdsService>(out var ads))
{
    ads.ShowBanner();
}
```

⚠️ **Không gọi `Get<T>` trong constructor** - thứ tự init không đảm bảo. Dùng trong `Start()` trở đi.

---

## EventBus

📁 `Assets/_Project/Scripts/Core/Events/EventBus.cs`
📦 `GameTemplate.Core.Events`

Pub/Sub type-safe, zero-alloc (event là struct).

### Methods

| Method | Mô tả |
|---|---|
| `Subscribe<T>(Action<T> handler)` | Đăng ký callback nhận event type T. |
| `Unsubscribe<T>(Action<T> handler)` | Huỷ đăng ký. **BẮT BUỘC** trong OnDisable/OnDestroy. |
| `Publish<T>(T evt)` | Gọi mọi handler đã subscribe type T. |
| `Clear<T>()` | Xoá mọi handler của type T (dùng cẩn thận). |

### Example

```csharp
// 1. Định nghĩa event (phải là struct, kế thừa IGameEvent)
public struct PlayerJumpedEvent : IGameEvent
{
    public float Power;
}

// 2. Subscribe
private void OnEnable()
{
    EventBus.Subscribe<PlayerJumpedEvent>(OnJump);
}

private void OnDisable()
{
    EventBus.Unsubscribe<PlayerJumpedEvent>(OnJump);
}

private void OnJump(PlayerJumpedEvent evt)
{
    Debug.Log($"Player jumped with power: {evt.Power}");
}

// 3. Publish (từ chỗ khác)
EventBus.Publish(new PlayerJumpedEvent { Power = 5f });
```

💡 **Common events sẵn có** (`CommonEvents.cs`): `GameStartedEvent`, `GamePausedEvent`, `GameOverEvent`, `LevelLoadedEvent`, `SceneLoadStartedEvent`, `SceneLoadCompletedEvent`.

---

## GameLog

📁 `Assets/_Project/Scripts/Core/Logger/GameLog.cs`
📦 `GameTemplate.Core.Logger`

Logger có category + tự strip release build (zero overhead production).

### Methods

| Method | Mô tả | Strip khi release? |
|---|---|---|
| `Info(LogCategory, string)` | Log info | ✅ Bị strip nếu không `ENABLE_GAME_LOG` |
| `Warning(LogCategory, string)` | Log warning | ✅ Bị strip |
| `Error(LogCategory, string)` | Log error | ❌ Luôn log (lỗi prod cần track) |

### Properties

| Property | Mô tả |
|---|---|
| `EnabledCategories` | Bitmask lọc category. Default `LogCategory.All`. |

### LogCategory enum

`Bootstrap`, `Audio`, `Save`, `UI`, `Input`, `Scene`, `Gameplay`, `Network`, `Ads`, `IAP`, `Analytics`, `All`.

### Example

```csharp
GameLog.Info(LogCategory.Audio, "Playing background music");
GameLog.Warning(LogCategory.Save, "Save file backup created");
GameLog.Error(LogCategory.Network, "API timeout after 30s");

// Filter: chỉ log Audio + Save, ẩn các category khác
GameLog.EnabledCategories = LogCategory.Audio | LogCategory.Save;
```

⚠️ Để bật log: thêm `ENABLE_GAME_LOG` vào Scripting Define Symbols.

---

## ObjectPool

📁 `Assets/_Project/Scripts/Core/Pooling/ObjectPool.cs`
📦 `GameTemplate.Core.Pooling`

Pool generic - tránh GC spike khi spawn/despawn liên tục. Hỗ trợ IPoolable callback, MaxSize cap, active tracking.

### Constructors

```csharp
// Đầy đủ (production)
ObjectPool<T>(T prefab, int prewarm, int maxSize, Transform parent = null, bool expandable = true)

// Backward compatible (code cũ vẫn work)
ObjectPool<T>(T prefab, int initialSize, Transform parent = null, bool expandable = true)
```

| Param | Mô tả |
|---|---|
| `prefab` | Component để Instantiate (vd: Bullet, Enemy) |
| `prewarm` / `initialSize` | Số object tạo sẵn lúc init (tránh GC spike lần đầu Get) |
| `maxSize` | Cap tối đa instance (active + available). 0 = unlimited |
| `parent` | Transform cha (organize hierarchy) |
| `expandable` | True = tự tạo thêm đến maxSize, false = return null khi cạn |

### Methods

| Method | Mô tả |
|---|---|
| `Get()` | Lấy 1 object active. Auto gọi `IPoolable.OnSpawnFromPool`. Return null nếu cạn + đạt MaxSize |
| `Release(T instance)` | Trả về pool. Auto gọi `IPoolable.OnDespawnToPool`. SetActive(false). |
| `DespawnAll()` | Release tất cả instance đang active. Vd: scene reset |
| `Clear()` | Destroy mọi instance, clear pool. Vd: scene unload |

### Properties

| Property | Mô tả |
|---|---|
| `CountActive` | Số instance đang được dùng (đã Get nhưng chưa Release) |
| `CountAvailable` | Số instance đang chờ trong pool (inactive) |
| `CountTotal` | Tổng (active + available) |
| `MaxSize` | Limit tối đa (int.MaxValue nếu unlimited) |
| `Prefab` | Prefab gốc (cho debug) |

### Example

```csharp
private ObjectPool<Bullet> _bulletPool;

void Awake()
{
    _bulletPool = new ObjectPool<Bullet>(
        bulletPrefab,
        prewarm: 50,
        maxSize: 200,
        parent: transform);
}

void Shoot()
{
    var bullet = _bulletPool.Get();
    if (bullet == null) return;  // Pool đã đạt MaxSize
    bullet.transform.position = muzzle.position;
    bullet.Fire(direction);
}

void OnBulletHit(Bullet bullet) => _bulletPool.Release(bullet);
```

Xem [Cookbook recipe #21](COOKBOOK.md#21-pool-object-để-spawn-enemy) và [#21b IPoolable](COOKBOOK.md#21b-ipoolable---object-tự-reset-state-khi-spawnđespawn).

---

## IPoolable

📁 `Assets/_Project/Scripts/Core/Pooling/IPoolable.cs`
📦 `GameTemplate.Core.Pooling`

Interface cho object tự reset state khi spawn/despawn từ pool.

### Methods

| Method | Trigger |
|---|---|
| `OnSpawnFromPool()` | Gọi sau khi `SetActive(true)`, trước khi trả về caller |
| `OnDespawnToPool()` | Gọi trước khi `SetActive(false)` |

ObjectPool tự gọi qua `GetComponents<IPoolable>()` → nhiều IPoolable trên cùng GameObject đều được gọi.

Xem [Cookbook recipe #21b](COOKBOOK.md#21b-ipoolable---object-tự-reset-state-khi-spawnđespawn).

---

## JsonSaveService

📁 `Assets/_Project/Scripts/Core/Save/JsonSaveService.cs`
📦 `GameTemplate.Core.Save`

Save/load JSON async, atomic write (chống corrupt khi user kill app).

### Interface `ISaveService`

| Method | Mô tả |
|---|---|
| `LoadAsync<T>(string key)` | Load. Return `new T()` nếu file chưa có hoặc lỗi parse. |
| `SaveAsync<T>(string key, T data)` | Save. Atomic write (file tạm + rename). |
| `HasSave(string key)` | Check file tồn tại. |
| `Delete(string key)` | Xoá file save. |

### Base class

```csharp
[Serializable]
public abstract class SaveDataBase
{
    public int SaveVersion = 1; // tăng khi đổi schema để migrate
}
```

### Example

```csharp
[Serializable]
public class PlayerData : SaveDataBase
{
    public int Coins;
    public int Level;
}

// Save
var save = ServiceLocator.Get<ISaveService>();
await save.SaveAsync("player", new PlayerData { Coins = 100, Level = 5 });

// Load
var data = await save.LoadAsync<PlayerData>("player");
Debug.Log($"Coins: {data.Coins}");

// Migration khi đổi schema:
if (data.SaveVersion < 2)
{
    data.NewField = "default";
    data.SaveVersion = 2;
    await save.SaveAsync("player", data);
}
```

⚠️ JsonUtility (Unity) chỉ serialize fields (không serialize properties). Field phải public hoặc có `[SerializeField]`.

📁 File save: `Application.persistentDataPath/Saves/<key>.json`. Mở folder qua menu **GameTemplate → Folder → Persistent Data Path**.

---

## IDailyResetService

📁 `Assets/_Project/Scripts/Core/Scheduling/DailyResetService.cs`
📦 `GameTemplate.Core.Scheduling`

Service check "đã sang ngày mới chưa" - dùng cho daily reward, daily quest, free chest mỗi ngày.

### Triết lý

CHỈ làm phần universal (check ngày, lưu timestamp, đếm streak). KHÔNG làm UI claim hay reward data - game tự build.

### Methods

| Method | Mô tả |
|---|---|
| `IsNewDay(string key)` | Hôm nay đã pass ngày của lần claim cuối chưa? |
| `MarkAsClaimed(string key)` | Đánh dấu đã claim hôm nay + auto tính streak |
| `GetLastClaimDate(string key)` | DateTime lần claim cuối |
| `GetStreakIfClaimed(string key)` | Số ngày streak hiện tại (liên tiếp) |
| `GetTimeUntilNextReset()` | TimeSpan tới 00:00 ngày mai (cho countdown UI) |
| `ResetKey(string key)` | Xoá data 1 key (cho cheat/test) |

### Multi-key

1 service quản nhiều daily key độc lập: `"login_bonus"`, `"daily_quest"`, `"free_chest"`, `"ad_limit"`...

### Persistence

Dùng PlayerPrefs với prefix `Daily_LastClaim_` và `Daily_Streak_`. Đơn giản, không cần ISaveService.

### Time source

Dùng `DateTime.Now` (device time). User chỉnh đồng hồ có thể hack - chấp nhận trade-off cho casual game.

### Example

Xem [Cookbook recipe #4b](COOKBOOK.md#4b-check-ngày-mới-daily-reset--login-bonus).

---

## TutorialSequencer

📁 `Assets/_Project/Scripts/Core/Tutorial/TutorialSequencer.cs`
📦 `GameTemplate.Core.Tutorial`

Engine chạy tuần tự các tutorial step, lưu tiến độ PlayerPrefs, hỗ trợ skip/resume. Tách "engine" (universal) khỏi "content" (game tự viết step).

### Constructor

```csharp
TutorialSequencer(string tutorialId, ITutorialOverlay overlay)
```

### Methods

| Method | Mô tả |
|---|---|
| `AddStep(ITutorialStep step)` | Builder - thêm step, return this để chain |
| `Start(bool forceRestart = false)` | Bắt đầu. Skip nếu đã hoàn thành (trừ forceRestart) |
| `Tick()` | Gọi mỗi frame từ Update - chạy step logic |
| `SkipAll()` | Skip toàn bộ, đánh dấu hoàn thành |

### Properties

| Property | Mô tả |
|---|---|
| `IsRunning` | Tutorial đang chạy? |
| `CurrentStep` | Step hiện tại |
| `TutorialId` | Id tutorial |

### Events

| Event | Khi nào |
|---|---|
| `OnCompleted` | Toàn bộ tutorial xong |
| `OnSkipped` | User skip |
| `OnStepCompleted` | Mỗi step xong (param: step) |

### Static methods

| Method | Mô tả |
|---|---|
| `IsCompleted(string tutorialId)` | Check đã hoàn thành chưa |
| `ResetTutorial(string tutorialId)` | Reset (cho cheat/test) |

### Example

Xem [Cookbook recipe #4c](COOKBOOK.md#4c-tutorial-system-step-sequencer--highlight).

---

## ITutorialStep

📁 `Assets/_Project/Scripts/Core/Tutorial/ITutorialStep.cs`
📦 `GameTemplate.Core.Tutorial`

Interface cho 1 bước tutorial. Engine chỉ biết 3 method, không quan tâm nội dung.

### Methods

| Method | Mô tả |
|---|---|
| `StepId` | Id duy nhất (cho save tiến độ) |
| `Enter(TutorialContext)` | Gọi 1 lần khi step bắt đầu |
| `IsComplete()` | Gọi mỗi frame, true = xong |
| `Exit()` | Gọi 1 lần khi step kết thúc |

### 4 built-in steps

| Step | Constructor | Complete khi |
|---|---|---|
| `MessageStep` | `(id, message, placement)` | User tap |
| `WaitForClickStep` | `(id, targetGetter, message, placement)` | Bấm đúng highlight |
| `WaitForEventStep<T>` | `(id, message, highlightGetter?, filter?)` | EventBus event fire |
| `WaitForConditionStep` | `(id, condition, message, highlightGetter?)` | Func bool true |

Viết step custom: implement `ITutorialStep`. Xem [Cookbook #4c](COOKBOOK.md#4c-tutorial-system-step-sequencer--highlight).

---

## ITutorialOverlay

📁 `Assets/_Project/Scripts/Core/Tutorial/ITutorialOverlay.cs` (interface), `TutorialOverlay.cs` (impl)
📦 `GameTemplate.Core.Tutorial`

UI overlay tutorial: mask tối + khoét lỗ highlight + text bubble + block input. Implement bằng `TutorialOverlay` MonoBehaviour (4 mask panel, không cần shader).

### Methods

| Method | Mô tả |
|---|---|
| `Show()` / `Hide()` | Bật/tắt overlay |
| `HighlightTarget(RectTransform, allowClickThrough, padding)` | Khoét lỗ vào UI element |
| `HighlightWorldPosition(Vector3, screenRadius, allowClickThrough)` | Highlight vị trí world space |
| `ClearHighlight()` | Mask phủ kín lại |
| `ShowMessage(message, placement, showTapHint)` | Hiện text bubble |
| `HideMessage()` | Ẩn bubble |
| `WasTappedThisFrame()` | User vừa tap overlay? |
| `WasHighlightTappedThisFrame()` | User tap đúng vùng highlight? |

### Enum `BubblePlacement`

`Auto`, `Above`, `Below`, `Left`, `Right`, `Center`.

### Setup

Tạo Canvas prefab với 4 mask panel + FullMask + MessageBubble, add `TutorialOverlay` component, kéo references. Xem [Cookbook #4c](COOKBOOK.md#4c-tutorial-system-step-sequencer--highlight).

---

## AudioManager

📁 `Assets/_Project/Scripts/Core/Audio/AudioManager.cs`
📦 `GameTemplate.Core.Audio`

Audio manager với pool AudioSource cho SFX (tránh GC).

### Interface `IAudioService`

**Mute API** (cho game không cần slider volume - hyper-casual, puzzle):

| Property | Mô tả |
|---|---|
| `IsMasterMuted` | Tắt toàn bộ âm thanh. Auto save PlayerPrefs. |
| `IsMusicMuted` | Tắt riêng nhạc. |
| `IsSfxMuted` | Tắt riêng SFX. |
| `ToggleMaster()` | Toggle nhanh, return state mới. |
| `ToggleMusic()` | Toggle music. |
| `ToggleSfx()` | Toggle SFX. |

**Volume API** (cho game có slider 0-1):

| Property | Mô tả |
|---|---|
| `MasterVolume` (get/set) | 0..1. Set auto save PlayerPrefs. |
| `MusicVolume` (get/set) | 0..1. |
| `SfxVolume` (get/set) | 0..1. |

**Event:**

| Event | Khi nào fire |
|---|---|
| `OnAudioSettingsChanged` | Bất kỳ thay đổi mute/volume nào - UI subscribe để refresh icon. |

**Playback:**

| Method | Mô tả |
|---|---|
| `PlaySfx(AudioClip, float volume = 1, float pitch = 1)` | Phát SFX qua pool. Skip nếu đang mute. |
| `PlayMusic(AudioClip, bool loop = true, float fadeIn = 1f)` | Phát nhạc với fade. |
| `StopMusic(float fadeOut = 1f)` | Dừng nhạc. |

**Mute vs Volume:**
- 2 thứ độc lập, mute không reset volume
- Unmute lại trả về volume cũ
- Master mute override cả music + sfx (kể cả volume riêng)

### Setup (Bootstrap)

1. Tạo Audio Mixer với 3 group: Master, Music, SFX
2. Expose 3 param: `MasterVolume`, `MusicVolume`, `SfxVolume`
3. Tạo prefab AudioManager với script `AudioManager.cs`, gán Mixer + Groups
4. Gán prefab vào `GameBootstrap.AudioManagerPrefab`

### Example

```csharp
var audio = ServiceLocator.Get<IAudioService>();

// Playback
audio.PlaySfx(jumpSound);
audio.PlaySfx(coinSound, volume: 0.5f, pitch: 1.2f);
audio.PlayMusic(bgmTheme, loop: true, fadeIn: 2f);
audio.StopMusic(fadeOut: 1f);

// Mute on/off (cho game không cần slider)
audio.IsMasterMuted = true;     // tắt toàn bộ
audio.ToggleMusic();            // toggle nhanh

// Volume 0-1 (cho game có slider)
audio.MasterVolume = 0.8f;
audio.SfxVolume = 0.5f;

// Subscribe để UI tự update khi setting đổi
audio.OnAudioSettingsChanged += () => RefreshSoundIcon();
```

**Settings tự persist PlayerPrefs** - reload game vẫn nhớ user đã mute/đặt volume bao nhiêu.

---

## SceneLoader

📁 `Assets/_Project/Scripts/Core/SceneManagement/SceneLoader.cs`
📦 `GameTemplate.Core.SceneManagement`

Load scene async + publish event cho loading UI.

### Interface `ISceneLoader`

| Method | Mô tả |
|---|---|
| `LoadSceneAsync(string sceneName, bool showLoading = true)` | Load async. Publish event nếu showLoading. |
| `Progress` | Float 0-1 progress hiện tại. |

### Events publish

- `SceneLoadStartedEvent` - khi bắt đầu load
- `SceneLoadCompletedEvent` - khi load xong

### Example

```csharp
// Load đơn giản
var loader = ServiceLocator.Get<ISceneLoader>();
await loader.LoadSceneAsync("Level_03");

// Loading UI subscribe
public class LoadingScreen : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Subscribe<SceneLoadStartedEvent>(OnLoadStarted);
        EventBus.Subscribe<SceneLoadCompletedEvent>(OnLoadCompleted);
    }
    // ...
}
```

---

## UIPanel + UIManager

📁 `Assets/_Project/Scripts/Core/UI/UIPanel.cs`, `UIManager.cs`
📦 `GameTemplate.Core.UI`

### UIPanel (base class)

| Method | Mô tả |
|---|---|
| `Show()` | Hiện panel + alpha 1. |
| `Hide()` | Ẩn panel + alpha 0. |
| `OnShow()` | Override để xử lý khi hiện. |
| `OnHide()` | Override để cleanup. |
| `OnBackPressed()` | Override để handle Android back. Return true = đã xử lý. |

### UIManager (stack-based)

| Method | Mô tả |
|---|---|
| `Push(UIPanel)` | Push panel mới, ẩn panel cũ. |
| `Pop()` | Quay lại panel trước. |
| `PopAll()` | Đóng hết panel. |
| `CurrentPanel` | Panel đang trên cùng stack. |

### Example

```csharp
// Tạo panel
public class SettingsPanel : UIPanel
{
    [SerializeField] private Slider _volumeSlider;

    protected override void OnShow()
    {
        _volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
    }

    public override bool OnBackPressed()
    {
        ServiceLocator.Get<UIManager>().Pop();
        return true;
    }
}

// Sử dụng
var ui = ServiceLocator.Get<UIManager>();
ui.Push(settingsPanel);
// User bấm back → UIManager auto Pop
```

---

## SafeAreaFitter

📁 `Assets/_Project/Scripts/Core/UI/SafeAreaFitter.cs`
📦 `GameTemplate.Core.UI`

MonoBehaviour component auto-fit RectTransform vào `Screen.safeArea` - tránh notch, home indicator, status bar.

### Fields

| Field | Default | Mô tả |
|---|---|---|
| `padTop` | true | Apply padding cho cạnh trên (status bar, notch) |
| `padBottom` | true | Apply padding cho cạnh dưới (home indicator) |
| `padLeft` | true | Apply padding cho cạnh trái (notch khi landscape) |
| `padRight` | true | Apply padding cho cạnh phải |
| `logChanges` | false | Debug: log mỗi lần re-apply |

### Behavior

- Auto-apply trong `Awake()`, `OnEnable()`, và `Update()` khi detect change
- Detect change: `Screen.safeArea`, `Screen.width/height`, `Screen.orientation`
- Hỗ trợ xoay device, foldable phone, split screen Android

### Setup

```
Canvas (full màn hình)
├── Background (Image)         ← ngoài SafeArea, tràn vào notch OK
└── SafeArea (SafeAreaFitter)  ← anchor stretch full, offset = 0
    ├── TopBar
    ├── GameplayUI
    └── BottomBar
```

### Example

Xem [Cookbook recipe #5b](COOKBOOK.md#5b-tạo-ui-an-toàn-với-notch--home-indicator-safe-area).

---

## CameraFitter

📁 `Assets/_Project/Scripts/Core/Camera/CameraFitter.cs`
📦 `GameTemplate.Core.Camera`

MonoBehaviour auto-fit Camera (Orthographic 2D hoặc Perspective 3D) vào design area trên mọi device aspect. Object luôn scale 1, camera tự điều chỉnh.

### Fields

| Field | Default | Mô tả |
|---|---|---|
| `designWidth` | 9 | Chiều ngang design area (world units) |
| `designHeight` | 16 | Chiều dọc design area |
| `mode` | `AutoByOrientation` | Strategy fit |
| `logChanges` | false | Debug log mỗi lần re-fit |
| `drawGizmo` | true | Vẽ design area trong Scene view |

### Enum `CameraFitMode`

| Value | Behavior | Phù hợp |
|---|---|---|
| `FitWidth` | Cố định chiều ngang = DesignWidth | Portrait, side-scroller, runner |
| `FitHeight` | Cố định chiều dọc = DesignHeight | Landscape, top-down |
| `AutoByOrientation` | Portrait → FitWidth, Landscape → FitHeight | Game support cả 2 orientation |

### Properties

| Property | Mô tả |
|---|---|
| `Camera` | Camera component được fit |
| `EffectiveMode` | Mode đang áp dụng (resolve Auto thành Width/Height) |

### Behavior

- Apply trong `Awake()`
- Re-apply trong `Update()` khi screen size đổi (xoay device, split screen, foldable)
- Support Orthographic (set `orthographicSize`) và Perspective (set `fieldOfView`)

### Example

Xem [Cookbook recipe #5c](COOKBOOK.md#5c-camera-fit-gameplay-area-trên-mọi-device-aspect).

---

## EnhancedButton

📁 `Assets/_Project/Scripts/Core/UI/Buttons/EnhancedButton.cs`
📦 `GameTemplate.Core.UI.Buttons`

Button kế thừa Unity `Button`, thêm SFX preset/custom + Analytics tracking (event name only) + Haptic + spam protection.

### Fields

| Field | Default | Mô tả |
|---|---|---|
| `sfxPreset` | `Click` | None/Click/Confirm/Cancel/Error/Custom |
| `customSfx` | null | Override AudioClip - dùng thay cho preset library nếu set |
| `sfxVolumeScale` | 1.0 | Scale volume 0..1 |
| `trackEventName` | "" | Tên Analytics event. Để trống = không track |
| `hapticType` | `Selection` | Loại haptic mobile (None để tắt) |
| `minIntervalBetweenClicks` | 0.2 | Anti-spam seconds. 0 = tắt |

### SFX priority

1. Preset = None → không phát
2. Custom Sfx ≠ null → phát Custom (override preset)
3. Preset = Click/Confirm/... → lookup `UIButtonSfxLibrary` qua ServiceLocator

### Public API

```csharp
SetTrackEvent(string eventName)   // Đổi event name runtime
```

### Example

Xem [Cookbook recipe #5d](COOKBOOK.md#5d-enhancedbutton---button-có-sfx--analytics--haptic).

---

## HoldButton

📁 `Assets/_Project/Scripts/Core/UI/Buttons/HoldButton.cs`
📦 `GameTemplate.Core.UI.Buttons`

Nút nhấn-giữ với 3 UnityEvent wire trực tiếp Inspector.

### Fields

| Field | Default | Mô tả |
|---|---|---|
| `onStartAction` | empty | Fire khi pointer down |
| `onHoldAction` | empty | Fire mỗi frame khi đang giữ |
| `onReleaseAction` | empty | Fire khi pointer up hoặc drag ra ngoài |
| `releaseOnPointerExit` | true | Drag ra ngoài = fire Release |
| `hapticOnStart` | `Light` | Haptic khi bắt đầu giữ |
| `hapticOnRelease` | `None` | Haptic khi nhả |

### Properties

| Property | Mô tả |
|---|---|
| `IsHolding` | True khi đang giữ |
| `HoldDuration` | Giây đã giữ (0 nếu không giữ) |
| `OnStartAction` / `OnHoldAction` / `OnReleaseAction` | UnityEvent public - AddListener trong code |

### Wire qua Inspector

Drag GameObject + chọn method public → không cần code subscribe.

### Example

Xem [Cookbook recipe #5e](COOKBOOK.md#5e-holdbutton---nút-nhấn-giữ-với-unityevent).

---

## UIButtonSfxLibrary

📁 `Assets/_Project/Scripts/Core/UI/Buttons/UIButtonSfxLibrary.cs`
📦 `GameTemplate.Core.UI.Buttons`

ScriptableObject map enum `ButtonSfxPreset` sang AudioClip. 1 asset cho cả project.

### Setup

1. Create asset: Project → Create → Game → UI → Button SFX Library
2. Kéo AudioClip cho Click, Confirm, Cancel, Error
3. Kéo asset vào `MobileServicesBootstrapper._uiButtonSfxLibrary`

Bootstrap tự đăng ký vào ServiceLocator → mọi `EnhancedButton` dùng được.

### Method

`AudioClip GetClip(ButtonSfxPreset preset)` - return null nếu preset không có clip.

---

## ReactiveProperty

📁 `Assets/_Project/Scripts/Core/Patterns/Reactive/ReactiveProperty.cs`
📦 `GameTemplate.Core.Patterns.Reactive`

Observable value - UI tự update khi value đổi.

### Methods

| Method/Property | Mô tả |
|---|---|
| `Value` (get/set) | Lấy/gán value. Set sẽ notify nếu khác value cũ. |
| `SetSilent(T value)` | Set không notify. Dùng khi load save. |
| `ForceNotify()` | Notify dù value không đổi. |
| `Subscribe(Action<T>)` | Đăng ký callback. |
| `SubscribeWithInit(Action<T>)` | Subscribe + gọi luôn với value hiện tại. |
| `Unsubscribe(Action<T>)` | Huỷ. |
| `ClearSubscribers()` | Xoá hết subscriber. |

### Example

```csharp
public class PlayerStats
{
    public ReactiveProperty<int> Hp = new ReactiveProperty<int>(100);
}

// UI side
_stats.Hp.SubscribeWithInit(hp => _hpText.text = $"HP: {hp}");

// Trong OnDisable
_stats.Hp.Unsubscribe(OnHpChanged);

// Update value (UI tự refresh)
_stats.Hp.Value = 80;

// Implicit conversion - dùng như value thường
int currentHp = _stats.Hp; // không cần .Value
```

---

## ReactiveCollection

📁 cùng file `ReactiveProperty.cs`

Observable list cho inventory, quest log...

### Events

| Event | Mô tả |
|---|---|
| `OnAdd(T item)` | Khi item add. |
| `OnRemove(T item)` | Khi item remove. |
| `OnClear()` | Khi clear all. |
| `OnChange()` | Bất kỳ thay đổi nào (Add/Remove/Clear). |

### Example

```csharp
public ReactiveCollection<Item> Inventory = new ReactiveCollection<Item>();

// UI
Inventory.OnAdd += item => SpawnSlot(item);
Inventory.OnRemove += item => DestroySlot(item);
Inventory.OnChange += () => UpdateWeightText();

// Modify
Inventory.Add(newSword);
Inventory.Remove(oldShield);
```

---

## PrefabFactory

📁 `Assets/_Project/Scripts/Core/Patterns/Factory/PrefabFactory.cs`
📦 `GameTemplate.Core.Patterns.Factory`

Spawn entity từ ScriptableObject data + có pool sẵn.

### Setup

```csharp
// 1. Data class kế thừa FactoryDataBase
[CreateAssetMenu(menuName = "Game/Enemy Data")]
public class EnemyData : FactoryDataBase
{
    public int MaxHp;
    public GameObject EnemyPrefab;
    public override GameObject Prefab => EnemyPrefab;
}

// 2. Entity implement IConfigurable<TData>
public class Enemy : MonoBehaviour, IConfigurable<EnemyData>
{
    public void Configure(EnemyData data) { /* set stats */ }
}

// 3. Tạo factory
var factory = new PrefabFactory<EnemyData, Enemy>(
    allEnemyData,
    poolParent: transform,
    initialPoolSize: 8
);
```

### Methods

| Method | Mô tả |
|---|---|
| `Create(string id, Vector3 pos, Quaternion rot = default)` | Spawn entity theo Id, auto pool. |
| `Return(string id, T entity)` | Trả về pool. |

### Example

```csharp
// Spawn slime tại pos
var slime = factory.Create("slime_lv1", spawnPos);

// Sau khi slime chết
factory.Return("slime_lv1", slime);
```

---

## MVP Base

📁 `Assets/_Project/Scripts/Core/Patterns/MVP/MVPBase.cs`
📦 `GameTemplate.Core.Patterns.MVP`

### `ViewBase`
MonoBehaviour base cho View. Override `Bind()` và `Unbind()`.

### `PresenterBase<TView, TModel>`
Pure C# class.

| Method | Mô tả |
|---|---|
| `Init()` | Subscribe Model, init UI. |
| `Dispose()` | Cleanup. |
| `OnInit()` | Override để add logic init. |
| `OnDispose()` | Override để cleanup. |

### Example đầy đủ

Xem `HpBarExample.cs` trong `Patterns/MVP/Examples/`.

---

## MonoSingleton

📁 `Assets/_Project/Scripts/Core/Patterns/Singleton/MonoSingleton.cs`
📦 `GameTemplate.Core.Patterns.Singleton`

MonoBehaviour singleton an toàn (lazy, thread-safe, tránh ghost khi quit).

⚠️ **Template ưu tiên ServiceLocator hơn Singleton.** Chỉ dùng khi cần static context.

### Example

```csharp
public class GameManager : MonoSingleton<GameManager>
{
    public int Score { get; set; }

    public void StartGame() { /* ... */ }
}

// Sử dụng
GameManager.Instance.StartGame();
```

### Pure C# Singleton

```csharp
public class ConfigCache : Singleton<ConfigCache>
{
    public Dictionary<string, string> Data = new Dictionary<string, string>();
}

ConfigCache.Instance.Data["key"] = "value";
```

---

## AsyncOp + Sequence

📁 `Assets/_Project/Scripts/Core/Patterns/Async/AsyncOp.cs`
📦 `GameTemplate.Core.Patterns.Async`

### `AsyncOp` static methods

| Method | Mô tả |
|---|---|
| `Delay(float seconds)` | Delay theo Time.deltaTime (pause khi game pause). |
| `DelayRealtime(float seconds)` | Delay realtime (không pause). |
| `WaitUntil(Func<bool> condition, float timeout = -1)` | Đợi đến khi điều kiện true. |
| `Tween(from, to, duration, onUpdate, curve = null)` | Tween float, gọi callback mỗi frame. |
| `WhenAll(params Task[])` | Chạy song song nhiều task. |
| `RepeatAction(float interval)` | Quick use - lặp `LoopAction` static. Chỉ 1 loop tại 1 thời điểm. |
| `RepeatAction(float interval, Action, CancellationToken, bool runImmediately = true)` | Production use - lặp action với cancel token. Exception-safe. |

### `Sequence` builder

| Method | Mô tả |
|---|---|
| `Append(Func<Task>)` | Thêm step. |
| `AppendDelay(float)` | Thêm delay. |
| `AppendCallback(Action)` | Thêm callback đồng bộ. |
| `Play()` | Chạy sequence (await). |

### Example

```csharp
// Delay
await AsyncOp.Delay(2f);

// Tween fade
await AsyncOp.Tween(0f, 1f, 0.3f, v => canvasGroup.alpha = v);

// Wait condition
await AsyncOp.WaitUntil(() => player.IsAtTarget, timeout: 10f);

// Sequence chain
await new Sequence()
    .Append(() => AsyncOp.Tween(0, 1, 0.3f, v => panel.alpha = v))
    .AppendDelay(2f)
    .Append(() => AsyncOp.Tween(1, 0, 0.3f, v => panel.alpha = v))
    .AppendCallback(() => panel.gameObject.SetActive(false))
    .Play();
```

---

## IAdsService

📁 `Assets/_Project/Scripts/Core/Mobile/Ads/IAdsService.cs`
📦 `GameTemplate.Core.Mobile.Ads`

### Properties

| Property | Mô tả |
|---|---|
| `CurrentMediation` | Mediation đang dùng. |
| `IsInitialized` | Đã init xong chưa. |
| `AdsEnabled` | Bật/tắt toàn bộ ads. |
| `BannerEnabled` | Bật/tắt riêng banner. |
| `InterstitialEnabled` | Bật/tắt riêng interstitial. |
| `RewardedEnabled` | Bật/tắt rewarded (không nên tắt). |
| `IsBannerVisible` | Banner đang hiện hay không. |

### Methods

| Method | Mô tả |
|---|---|
| `InitializeAsync()` | Init SDK. |
| `ShowBanner(BannerPosition = Bottom)` | Hiện banner. |
| `HideBanner()` | Ẩn banner. |
| `IsInterstitialReady()` | Check ad có sẵn không. |
| `ShowInterstitialAsync(string placement)` | Show full-screen ad. Return AdResult. |
| `IsRewardedReady()` | Check rewarded có sẵn. |
| `ShowRewardedAsync(string placement)` | Show rewarded video. Return AdResult. |

### Events

`OnAdShown(string)`, `OnAdClicked(string)`, `OnRewardEarned(string)`.

### Enum `AdResult`

`Success`, `Failed`, `Skipped`, `Closed`, `NotReady`.

### Example

```csharp
var ads = ServiceLocator.Get<IAdsService>();

// Banner
ads.ShowBanner(BannerPosition.Bottom);
ads.HideBanner();

// Interstitial
if (ads.IsInterstitialReady())
{
    await ads.ShowInterstitialAsync("level_complete");
}

// Rewarded
var result = await ads.ShowRewardedAsync("revive");
if (result == AdResult.Success)
{
    GivePlayerReward();
}

// Tắt khi user mua Remove Ads
ads.AdsEnabled = false;
```

---

## IIapService

📁 `Assets/_Project/Scripts/Core/Mobile/IAP/IIapService.cs`

### Methods

| Method | Mô tả |
|---|---|
| `InitializeAsync(IEnumerable<ProductInfo>)` | Init với list product. |
| `GetProducts()` | List mọi product. |
| `GetProduct(string id)` | Lấy product theo ID. |
| `IsOwned(string id)` | Check non-consumable đã mua chưa. |
| `PurchaseAsync(string id)` | Mua. Return PurchaseResult. |
| `RestorePurchasesAsync()` | Restore (BẮT BUỘC iOS). |

### Class `ProductInfo`

```csharp
public class ProductInfo
{
    public string ProductId;
    public ProductType Type; // Consumable, NonConsumable, Subscription
    public string LocalizedPrice;
    public string LocalizedTitle;
    public string LocalizedDescription;
    public bool IsOwned;
}
```

### Example

Xem [Cookbook recipe #16](COOKBOOK.md#16-mua-in-app-purchase).

---

## IAnalyticsService

📁 `Assets/_Project/Scripts/Core/Mobile/Analytics/AnalyticsService.cs`

Multi-provider analytics (Firebase + GameAnalytics + Mock... đều track cùng lúc).

> 💡 **Tại sao `Dictionary<string, object>` thay vì `Parameter[]` (Firebase)?**
>
> Template dùng **Adapter pattern** - interface vendor-agnostic, mỗi provider tự convert sang SDK format riêng. Lợi ích: code gameplay không phụ thuộc Firebase, dễ swap sang GameAnalytics/AppsFlyer, build được không cần SDK.
>
> Chi tiết + code FirebaseAnalyticsProvider mẫu: xem [MOBILE_INTEGRATION.md](MOBILE_INTEGRATION.md#analytics---adapter-pattern-giải-thích)

### Methods

| Method | Mô tả |
|---|---|
| `RegisterProvider(IAnalyticsProvider)` | Add destination. |
| `SetUserId(string)` | Identify user. |
| `SetUserProperty(string key, string value)` | Set property (vd: level, country). |
| `TrackEvent(string name)` | Event không param. |
| `TrackEvent(string name, Dictionary<string, object>)` | Event với param. |

### Helper events chuẩn

| Method | Event name | Params |
|---|---|---|
| `TrackLevelStart(int level)` | `level_start` | level |
| `TrackLevelComplete(int, float)` | `level_complete` | level, duration |
| `TrackLevelFail(int, string)` | `level_fail` | level, reason |
| `TrackAdShown(string, string)` | `ad_shown` | placement, ad_type |
| `TrackPurchase(string, string, float)` | `purchase` | product_id, currency, price |

### Example

```csharp
var analytics = ServiceLocator.Get<IAnalyticsService>();

analytics.SetUserId("user_12345");
analytics.SetUserProperty("country", "VN");
analytics.SetUserProperty("highest_level", "42");

analytics.TrackEvent("tutorial_skip");
analytics.TrackLevelComplete(levelIndex: 5, durationSeconds: 120f);
analytics.TrackEvent("button_click", new Dictionary<string, object>
{
    ["button"] = "shop",
    ["screen"] = "main_menu"
});
```

---

## IRemoteConfigService

📁 `Assets/_Project/Scripts/Core/Mobile/RemoteConfig/IRemoteConfigService.cs`

### Methods

| Method | Mô tả |
|---|---|
| `FetchAsync()` | Fetch config từ server. |
| `SetDefaults(Dictionary)` | Default values khi offline. |
| `GetString(key, default = "")` | Get string value. |
| `GetInt(key, default = 0)` | Get int. |
| `GetLong(key, default = 0)` | Get long. |
| `GetFloat(key, default = 0)` | Get float. |
| `GetBool(key, default = false)` | Get bool. |

### Event

`OnConfigUpdated` - khi config fetch xong/update.

### Example

```csharp
var config = ServiceLocator.Get<IRemoteConfigService>();

bool newShopOn = config.GetBool("new_shop_enabled", false);
int dailyReward = config.GetInt("daily_reward_amount", 100);
float coinMult = config.GetFloat("coin_drop_multiplier", 1.0f);
```

---

## ILocalizationService

📁 `Assets/_Project/Scripts/Core/Mobile/Localization/LocalizationService.cs`

### Methods

| Method | Mô tả |
|---|---|
| `Get(string key, string fallback = null)` | Lấy text. Auto fallback English nếu key không có ở ngôn ngữ hiện tại. |
| `Get(string key, params object[] args)` | Format string với placeholder `{0}`, `{1}`. |
| `SetLanguage(GameLanguage)` | Đổi ngôn ngữ. Trigger `OnLanguageChanged`. |

### Properties

| Property | Mô tả |
|---|---|
| `CurrentLanguage` | GameLanguage hiện tại. |
| `AvailableLanguages` | List ngôn ngữ có trong table. |

### Event

`OnLanguageChanged` - khi đổi ngôn ngữ.

### Enum `GameLanguage`

`English`, `Vietnamese`, `Japanese`, `Korean`, `ChineseSimplified`, `ChineseTraditional`, `Spanish`, `French`, `German`, `Italian`, `Portuguese`, `Russian`, `Thai`, `Indonesian`.

### Workflow

1. Tạo CSV: `Key,English,Vietnamese\nui.play,Play,Chơi`
2. Menu **GameTemplate → Import → CSV to Localization Table**
3. Gán `LocalizationTable.asset` vào `MobileServicesBootstrapper`

### Example

```csharp
var loc = ServiceLocator.Get<ILocalizationService>();

string text = loc.Get("ui.play");                    // "Chơi"
string score = loc.Get("ui.score_format", 100);      // "Score: 100"

loc.SetLanguage(GameLanguage.English);
```

---

## IHapticService

📁 `Assets/_Project/Scripts/Core/Mobile/Haptic/HapticService.cs`

### Methods

| Method | Mô tả |
|---|---|
| `Play(HapticType)` | Rung theo type. |

### Properties

| Property | Mô tả |
|---|---|
| `IsEnabled` (get/set) | Bật/tắt rung. Auto save PlayerPrefs. |
| `IsSupported` | Device có support rung không. |

### Enum `HapticType`

`Light`, `Medium`, `Heavy`, `Success`, `Warning`, `Failure`, `Selection`.

### Example

```csharp
var haptic = ServiceLocator.Get<IHapticService>();

haptic.Play(HapticType.Light);  // tap UI
haptic.Play(HapticType.Success); // win level
haptic.IsEnabled = false;       // user setting
```

---

## IDeviceInfoService

📁 `Assets/_Project/Scripts/Core/Mobile/Device/DeviceInfoService.cs`

### Properties

| Property | Mô tả |
|---|---|
| `Tier` | DeviceTier.Low/Mid/High |
| `SystemMemoryMb` | RAM device. |
| `GraphicsMemoryMb` | VRAM. |
| `DeviceModel` | Model name. |
| `OsVersion` | OS version. |
| `IsLowEndDevice` | True nếu Tier = Low. |

### Method

`ApplyTierSettings()` - apply QualitySettings theo tier.

### Example

```csharp
var device = ServiceLocator.Get<IDeviceInfoService>();

if (device.IsLowEndDevice)
{
    // Tắt VFX nặng, giảm enemy count
    maxEnemiesOnScreen = 10;
}
else
{
    maxEnemiesOnScreen = 30;
}
```

---

## CheatConsole

📁 `Assets/_Project/Scripts/Core/DevTools/CheatConsole.cs`
📦 `GameTemplate.Core.DevTools`

Console runtime, chỉ có trong Editor + Development Build.

### Toggle

- Editor: `Tab` key
- Mobile dev build: 3-finger tap

### Register command

**Cách 1 - Manual:**
```csharp
CheatConsole.RegisterCommand("kill_all", "Diệt mọi enemy", args =>
{
    foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        e.Kill();
});
```

**Cách 2 - Attribute (auto discover):**
```csharp
public static class GameplayCheats
{
    [CheatCommand("god_mode", "Toggle god mode")]
    static void GodMode(string[] args)
    {
        Player.Instance.IsGod = !Player.Instance.IsGod;
    }
}
```

### Built-in commands

`help`, `clear`, `close`, `fps`, `scene`, `time_scale <value>`.

⚠️ Release build production: console hoàn toàn không tồn tại (compile strip).

---

## SaveInspector

📁 `Assets/_Project/Scripts/Core/DevTools/SaveInspector.cs`

Window xem/sửa save file JSON ngay trong build.

### Toggle

- Editor: `F2`
- Mobile dev build: 4-finger tap

### Tính năng

- List mọi file save trong `Saves/`
- Click file → xem nội dung JSON
- Edit trực tiếp → bấm Save Changes
- Delete file

Use case: test edge case "hp = 0", "coin overflow", "level 999" không cần replay game.

---

## FpsDisplay

📁 `Assets/_Project/Scripts/Core/DevTools/FpsDisplay.cs`
📦 `GameTemplate.Core.DevTools`

MonoBehaviour hiện FPS counter trên màn hình - dùng khi test performance trên device thật.

### Fields chính

| Field | Default | Mô tả |
|---|---|---|
| `fpsText` | null | Text UI để hiện. Null + autoCreateUI = tự tạo |
| `autoCreateUI` | true | Tự tạo Canvas + Text nếu fpsText null |
| `corner` | TopRight | 4 góc màn hình |
| `showAverage` | true | Hiện FPS trung bình mượt |
| `showMinMax` | false | Hiện min/max FPS trong 1 period |
| `showMemory` | false | Hiện RAM usage (managed) |
| `updateInterval` | 0.5 | Update text mỗi N giây |
| `goodFps` | 60 | Ngưỡng FPS tốt (xanh) |
| `warningFps` | 30 | Ngưỡng FPS warning (vàng), <30 = đỏ |
| `toggleKey` | BackQuote | Phím toggle (PC/Editor) |
| `toggleFingerCount` | 3 | Số ngón tap để toggle (mobile) |

### Public API

```csharp
Toggle()    // Bật/tắt visibility
Show()      // Force show
Hide()      // Force hide
```

### Example

Xem [Cookbook recipe #28b](COOKBOOK.md#28b-hiện-fps-counter-khi-test-trên-device).

---

## Tham khảo thêm

- [COOKBOOK.md](COOKBOOK.md) - 30+ công thức copy-paste
- [DIAGRAMS.md](DIAGRAMS.md) - Diagrams kiến trúc
- [ARCHITECTURE.md](ARCHITECTURE.md) - Chi tiết kiến trúc
- [PATTERNS.md](PATTERNS.md) - Khi nào dùng pattern nào
- [MOBILE_INTEGRATION.md](MOBILE_INTEGRATION.md) - Tích hợp SDK
- [EDITOR_TOOLS.md](EDITOR_TOOLS.md) - Editor tools
- [CODING_STANDARDS.md](CODING_STANDARDS.md) - Coding standards
- [BUILD_CHECKLIST.md](BUILD_CHECKLIST.md) - Trước khi ship
