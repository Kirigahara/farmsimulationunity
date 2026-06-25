# Architecture

Template này dùng kiến trúc **3 lớp + Service Locator + Event Bus**, tối ưu cho mobile và team nhỏ.

## Tổng quan 3 lớp

```
┌─────────────────────────────────────────────┐
│  Bootstrap (entry point, 1 scene)           │
│  GameBootstrap.cs - khởi tạo mọi service    │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  Core (framework, tái dùng nhiều game)      │
│  - ServiceLocator (DI nhẹ)                  │
│  - EventBus (giao tiếp module)              │
│  - SaveService, SceneLoader, AudioManager   │
│  - ObjectPool, UIPanel, GameLog             │
└──────────────────┬──────────────────────────┘
                   │ (dùng qua interface)
                   ▼
┌─────────────────────────────────────────────┐
│  Gameplay (game-specific)                   │
│  - PlayerController, Enemy, GameRules...    │
│  - Subscribe EventBus, request Service      │
└─────────────────────────────────────────────┘
```

## 1. Bootstrap Pattern

**Vấn đề:** Test 1 scene riêng (vd `Gameplay`) sẽ bị null reference vì các Manager chưa khởi tạo.

**Cách giải:**
- Scene `Bootstrap` là scene đầu tiên (index 0 trong Build Settings)
- `GameBootstrap.cs` chạy với `[DefaultExecutionOrder(-1000)]` để Awake trước mọi thứ
- Khởi tạo: SaveService → SceneLoader → AudioManager → UIManager → register vào ServiceLocator
- Sau đó tự load Main Menu

**Test scene giữa:** GameBootstrap detect nếu đã bootstrapped sẽ skip → an toàn khi dev play từ scene gameplay trực tiếp.

## 2. Service Locator (DI nhẹ)

`ServiceLocator` là dictionary static `Type → object`. Đăng ký trong Bootstrap, dùng ở mọi nơi:

```csharp
// Register (chỉ ở Bootstrap)
ServiceLocator.Register<IAudioService>(audioManager);

// Use (ở gameplay)
var audio = ServiceLocator.Get<IAudioService>();
audio.PlaySfx(jumpClip);
```

**Tại sao không dùng Zenject?**
- Mobile cần startup nhanh, code nhỏ → Service Locator đủ cho team 2-5 người
- Zenject mạnh nhưng overhead lớn, learning curve cao
- Nếu sau này cần DI thực sự (factory, scope) → migrate sang VContainer (nhẹ hơn Zenject)

## 3. Event Bus

Các module giao tiếp qua event, **không reference trực tiếp** nhau. Vd PlayerController không cần biết AudioManager:

```csharp
// Định nghĩa event (struct -> không GC)
public struct PlayerJumpedEvent : IGameEvent { public float Power; }

// Publish (player)
EventBus.Publish(new PlayerJumpedEvent { Power = 5f });

// Subscribe (audio, particle, UI...)
EventBus.Subscribe<PlayerJumpedEvent>(OnPlayerJumped);
```

**Quan trọng:** Mọi Subscribe phải có Unsubscribe trong `OnDestroy` để tránh leak.

## 4. Flow scene chuẩn

```
Bootstrap (1 lần duy nhất)
  → Init services
  → SceneLoader.LoadSceneAsync("MainMenu")

MainMenu
  → User bấm Play
  → SceneLoader.LoadSceneAsync("Gameplay")
  → EventBus publish SceneLoadStartedEvent
  → LoadingPanel.Show() (subscribe sẵn event đó)

Gameplay
  → EventBus publish SceneLoadCompletedEvent
  → LoadingPanel.Hide()
  → User chơi
  → Game Over → EventBus publish GameOverEvent
  → UI hiện popup, Audio play sfx, Save service ghi score
```

## 5. Save data versioning

Khi đổi struct save (vd thêm field), tăng `SaveVersion`. Code load kiểm tra version và migrate:

```csharp
var data = await save.LoadAsync<PlayerData>("player");
if (data.SaveVersion < 2)
{
    data.NewField = defaultValue;
    data.SaveVersion = 2;
    await save.SaveAsync("player", data);
}
```

## 6. Module độc lập qua Assembly Definitions

```
GameTemplate.Core.asmdef       (không reference gì)
GameTemplate.Gameplay.asmdef   (reference Core)
GameTemplate.Editor.asmdef     (reference cả 2, editor-only)
```

Lợi ích:
- Compile nhanh: sửa Gameplay không compile lại Core
- Ép kỷ luật: Core không phụ thuộc Gameplay → tái dùng được
- Test isolate dễ hơn

## 7. Best practices mobile cần nhớ

| Vấn đề | Giải pháp |
|---|---|
| GC spike | Object pool mọi spawn/despawn, dùng struct cho event, không alloc trong Update |
| Draw call | Sprite atlas, dynamic batching, tắt shadow trên enemy |
| Battery | targetFrameRate = 60 (30 cho idle game), Application.runInBackground = false |
| Startup time | Tránh Resources.Load đồng bộ ở Bootstrap → dùng async load |
| Save corrupt | Atomic write (file tạm + rename) - đã làm trong JsonSaveService |
| Memory | Unload scene + Resources.UnloadUnusedAssets() khi đổi scene lớn |

## Khi nào sửa Core, khi nào sửa Gameplay?

**Sửa Core khi:**
- Thêm capability dùng cho mọi game (vd: Ads service, Analytics service, IAP)
- Fix bug trong framework
- Cải thiện performance core systems

**Sửa Gameplay khi:**
- Thêm enemy mới, level mới, mechanic mới
- Logic chỉ game này có (vd: card matching, RPG combat)

**Quy tắc vàng:** Code Core không được `using GameTemplate.Gameplay`. Nếu thấy cần → có gì đó sai, refactor lại.
