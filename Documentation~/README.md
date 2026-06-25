# Unity Mobile Game Template

Template Unity cho game mobile (hyper-casual, puzzle, RPG) - team 2-5 người.

## Yêu cầu

- Unity 2022.3 LTS trở lên (recommend 2022.3 hoặc 6000.x LTS)
- Module: Android Build Support + iOS Build Support
- IDE: Visual Studio, Rider hoặc VSCode

## Setup lần đầu

1. Clone repo
2. Mở Unity Hub → Add project → chọn folder vừa clone
3. Mở Unity, đợi import xong (lần đầu sẽ lâu ~5-10 phút)
4. Mở scene **`Assets/_Project/Scenes/Bootstrap.unity`** (chưa có - đọc Documentation/ONBOARDING.md để tạo)
5. File → Build Settings → kéo Bootstrap lên đầu (index 0)
6. Bấm Play - game phải tự load từ Bootstrap → MainMenu

## Cấu trúc nhanh

```
Assets/_Project/      ← Tất cả code/asset của game
├── Scripts/Core/     ← Framework tái dùng (KHÔNG sửa khi làm game mới)
├── Scripts/Gameplay/ ← Code game cụ thể
├── Scenes/           ← Bootstrap + MainMenu + các level
├── Prefabs/Systems/  ← AudioManager, UIManager prefab
└── Settings/         ← ScriptableObject config

Documentation~/       ← Đọc trước khi code (dấu ~ để Unity bỏ qua)
├── ARCHITECTURE.md   ← Cách các hệ thống nối với nhau
├── ONBOARDING.md     ← Bắt đầu từ đâu
└── CODING_STANDARDS.md ← Quy tắc code chung
```

## Tạo game mới từ template này

1. Clone repo này thành repo mới (đổi remote)
2. Xóa nội dung (giữ folder + .gitkeep):
   - `Assets/_Project/Scripts/Gameplay/`
   - `Assets/_Project/Art/`
   - `Assets/_Project/Scenes/Gameplay/`
3. Đổi `Edit → Project Settings → Player`:
   - Company Name, Product Name, Package Name, Version
4. Đổi namespace `GameTemplate` → tên game (vd `MyGame`) nếu muốn
5. Bắt đầu code trong `Scripts/Gameplay/`

## Các lệnh quan trọng

- **Play từ Bootstrap mọi lúc**: bật `Edit → Preferences → SceneAutoLoader` (cần plugin) hoặc tạo menu item tự load.
- **Bật log dev**: Project Settings → Player → Scripting Define Symbols → thêm `ENABLE_GAME_LOG`. Bỏ ra khi build release.
- **Build mobile**: File → Build Settings → Android/iOS → Player Settings checklist trong `Documentation~/BUILD_CHECKLIST.md`.

## Documentation

Đọc theo thứ tự nếu mới vào dự án:
1. `Documentation~/ONBOARDING.md` - bắt đầu từ đâu (~15 phút)
2. `Documentation~/ARCHITECTURE.md` - hiểu các hệ thống
3. `Documentation~/CODING_STANDARDS.md` - quy tắc code

## Hỏi ai

- Architecture/Core: [Tech Lead]
- Gameplay: [Game Designer + lead programmer]
- Art pipeline: [Art Lead]
