# Onboarding - Dev mới vào dự án

Đọc file này nếu bạn vừa được add vào team. Mục tiêu: trong 1 buổi, hiểu kiến trúc và bắt đầu commit được.

## Bước 1: Setup (30 phút)

Theo `README.md` để clone + open project. Nếu Unity báo lỗi compile, đọc Console - thường là thiếu package, vào `Window → Package Manager` install lại.

## Bước 2: Chạy game lần đầu (10 phút)

- Mở scene `Bootstrap.unity`
- Bấm Play
- Quan sát Console - phải có log `[Bootstrap] All services registered.` và `[Scene] Loaded scene: MainMenu`
- Nếu lỗi → check Inspector của GameBootstrap, các prefab AudioManager/UIManager đã gán chưa

## Bước 3: Đọc ARCHITECTURE.md (20 phút)

Quan trọng nhất. Đọc kỹ phần:
- Bootstrap Pattern
- Service Locator
- Event Bus

Không cần nhớ chi tiết - cứ biết "à, có cái này ở đó" là đủ. Khi code sẽ tra lại.

## Bước 4: Hands-on - viết 1 feature nhỏ (1-2 tiếng)

Bài tập làm quen: **Thêm tính năng "Tap để nhảy + phát SFX + tăng score".**

### a. Thêm Event

`Assets/_Project/Scripts/Gameplay/Events/GameplayEvents.cs`:

```csharp
using GameTemplate.Core.Events;

namespace GameTemplate.Gameplay.Events
{
    public struct PlayerJumpedEvent : IGameEvent { }
    public struct ScoreChangedEvent : IGameEvent { public int NewScore; }
}
```

### b. Player Controller

`Assets/_Project/Scripts/Gameplay/Player/PlayerController.cs`:

```csharp
using UnityEngine;
using GameTemplate.Core.Events;
using GameTemplate.Gameplay.Events;

namespace GameTemplate.Gameplay.Player
{
    public class PlayerController : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) // tap
            {
                EventBus.Publish(new PlayerJumpedEvent());
            }
        }
    }
}
```

### c. Audio Reactor (sub event, không reference player)

```csharp
using UnityEngine;
using GameTemplate.Core.DI;
using GameTemplate.Core.Events;
using GameTemplate.Core.Audio;
using GameTemplate.Gameplay.Events;

namespace GameTemplate.Gameplay.Audio
{
    public class GameplayAudioReactor : MonoBehaviour
    {
        [SerializeField] private AudioClip _jumpClip;

        private void OnEnable()  => EventBus.Subscribe<PlayerJumpedEvent>(OnJump);
        private void OnDisable() => EventBus.Unsubscribe<PlayerJumpedEvent>(OnJump);

        private void OnJump(PlayerJumpedEvent _)
        {
            ServiceLocator.Get<IAudioService>().PlaySfx(_jumpClip);
        }
    }
}
```

### d. Score Manager

```csharp
using UnityEngine;
using GameTemplate.Core.Events;
using GameTemplate.Gameplay.Events;

namespace GameTemplate.Gameplay.Score
{
    public class ScoreManager : MonoBehaviour
    {
        private int _score;

        private void OnEnable()  => EventBus.Subscribe<PlayerJumpedEvent>(OnJump);
        private void OnDisable() => EventBus.Unsubscribe<PlayerJumpedEvent>(OnJump);

        private void OnJump(PlayerJumpedEvent _)
        {
            _score++;
            EventBus.Publish(new ScoreChangedEvent { NewScore = _score });
        }
    }
}
```

### Quan sát

- 3 file trên **không reference nhau** - chỉ qua Event và Service.
- Thêm class mới (vd VfxReactor để spawn particle khi nhảy) không cần sửa code cũ.
- Đây chính là **lý do dùng Event Bus** - tránh "ai cũng gọi ai".

## Bước 5: Convention git (10 phút)

- Branch: `feature/jump-mechanic`, `bugfix/audio-crash-android`
- Commit message: `[Gameplay] Add player jump`, `[Core] Fix save corruption on iOS`
- PR phải có:
  - Mô tả ngắn
  - Screenshot/video nếu là feature UI
  - Reviewer trong team

## Bước 6: Hỏi (mọi lúc)

Đừng ngại hỏi. Một câu hỏi 30 giây có thể tiết kiệm 3 tiếng debug.

## Checklist hoàn thành onboarding

- [ ] Chạy được Bootstrap → MainMenu không lỗi
- [ ] Hiểu được flow Service Locator + Event Bus
- [ ] Làm xong bài tập "Tap to jump" và demo cho mentor
- [ ] Biết folder nào đặt code gì
- [ ] Đã commit ít nhất 1 PR đúng convention
