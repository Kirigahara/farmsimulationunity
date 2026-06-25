# Unity Mobile Game Template

Template Unity boilerplate cho game mobile (hyper-casual, puzzle, RPG), team 2-5 người.

## 📚 Documentation - Đọc theo thứ tự phù hợp

### Overview
1. **[ONBOARDING.md](Documentation~/ONBOARDING.md)** - Setup + bài tập "Tap to jump" hands-on
2. **[COOKBOOK.md](Documentation~/COOKBOOK.md)** ⭐ - 30+ công thức "muốn làm X thì làm sao" (recipe ngắn, copy-paste)
3. **[DIAGRAMS.md](Documentation~/DIAGRAMS.md)** - Hiểu kiến trúc qua hình (GitHub auto-render Mermaid)
4. **[API_REFERENCE.md](Documentation~/API_REFERENCE.md)** - Chi tiết mọi class/method/parameter
5. **[ARCHITECTURE.md](Documentation~/ARCHITECTURE.md)** - Deep dive kiến trúc
6. **[PATTERNS.md](Documentation~/PATTERNS.md)** - Design patterns: khi nào dùng cái nào
7. **[MOBILE_INTEGRATION.md](Documentation~/MOBILE_INTEGRATION.md)** - Tích hợp Ads/IAP/Analytics SDK
8. **[EDITOR_TOOLS.md](Documentation~/EDITOR_TOOLS.md)** - Universal editor tools + cách extend
9. **[CODING_STANDARDS.md](Documentation~/CODING_STANDARDS.md)** - Quy tắc code
10. **[BUILD_CHECKLIST.md](Documentation~/BUILD_CHECKLIST.md)** - Trước khi build release

## ⭐ Example

Click thẳng vào recipe cụ thể:
- [Phát SFX](Documentation~/COOKBOOK.md#1-lấy-audiomanager-để-phát-sfx) - 10 dòng code
- [Save/Load player data](Documentation~/COOKBOOK.md#4-lưuđọc-dữ-liệu-player)
- [Show rewarded ads](Documentation~/COOKBOOK.md#14-show-rewarded-video)
- [Tự update UI khi data đổi](Documentation~/COOKBOOK.md#11-hpscore-tự-update-lên-ui)
- [Tutorial chain action](Documentation~/COOKBOOK.md#25-đợi-đến-khi-player-chạm-điểm)
- [Thêm cheat command](Documentation~/COOKBOOK.md#26-thêm-cheat-command-god-mode-add-coin)

## Tóm tắt template

- **Bootstrap pattern**: 1 scene khởi tạo mọi service, test scene giữa vẫn chạy
- **Service Locator + Event Bus**: DI nhẹ, modules không phụ thuộc trực tiếp
- **Mobile-first**: object pool, zero-alloc event, atomic save, audio pool
- **Assembly Definitions**: Core / Gameplay / Editor tách biệt, compile nhanh
- **Sẵn sàng**: SaveService, AudioManager, SceneLoader, UIManager, ObjectPool, GameLog
- **Mobile services**: Ads/IAP/Analytics/RemoteConfig/Localization/Haptic + Mock (chạy được không cần SDK)
- **Editor tools**: Scene Auto-Loader, Define Manager, Build Pipeline, CSV importer, SO Browser, Cheat Console, Save Inspector
- **Patterns**: Reactive, Factory, MVP, Singleton, Async helpers, Inspector attributes


