# Design Patterns - Khi nào dùng cái nào

Template có sẵn 5 pattern. Mỗi pattern giải quyết 1 nhóm vấn đề khác nhau - đừng dùng tất cả cho mọi thứ.

## 1. Reactive Property

**Vấn đề:** Mỗi lần `score++` phải nhớ gọi `UpdateScoreUI()`. Quên 1 chỗ → UI lệch data. Có 5 UI khác nhau cần score → phải gọi 5 method.

**Giải pháp:** `ReactiveProperty<int>` - UI subscribe vào, mỗi khi value đổi tự update.

**Khi nào dùng:**
- ✅ HP, MP, XP, Coins, Score, Level (data thay đổi liên tục, nhiều UI cần biết)
- ✅ Inventory dùng `ReactiveCollection<Item>` để UI tự refresh khi add/remove
- ❌ Data tĩnh load 1 lần (vd: tên player, ngày tạo account) - dùng property thường

**Quy tắc:**
- Subscribe trong `OnEnable`, Unsubscribe trong `OnDisable` (giống Event Bus)
- Dùng `SubscribeWithInit` để vừa subscribe vừa init UI với value hiện tại
- Khi load save dùng `SetSilent` để không trigger UI animation

## 2. Factory + Builder

**Vấn đề:** Spawn enemy có 20 loại, mỗi loại có prefab + stats khác nhau. Hard-code if/else theo type = code rối, designer không thêm enemy mới được.

**Giải pháp:**
- **Factory**: Designer tạo `EnemyData` ScriptableObject. Factory lookup theo Id, spawn từ pool.
- **Builder**: Khi spawn cần nhiều tham số tùy chọn (level, modifier, loot) → chain method dễ đọc.

**Khi nào dùng Factory:**
- ✅ Có nhiều variant cùng base type (enemy, item, projectile, particle effect)
- ✅ Data thay đổi theo balance design (designer chỉnh ScriptableObject, không cần coder)
- ❌ 1-2 loại object → tạo trực tiếp, factory là overkill

**Khi nào dùng Builder:**
- ✅ Constructor có > 4 tham số tùy chọn
- ✅ Object tạo qua nhiều bước (vd: spawn enemy + apply modifier + add loot table)
- ❌ Object đơn giản → dùng constructor thường

## 3. MVP (Model-View-Presenter)

**Vấn đề:** UI script vừa fetch data, vừa update Text, vừa xử lý click button, vừa gọi Save... → 500 dòng trong 1 file, không test được, đổi UI là phải sửa logic.

**Giải pháp:** Tách 3 phần:
- **Model**: data + business logic (pure C#)
- **View**: chỉ là MonoBehaviour với Text, Image, Button (không logic)
- **Presenter**: cầu nối, chứa logic UI

**Khi nào dùng:**
- ✅ UI panel phức tạp: Inventory, Shop, Quest log, Settings, Character sheet
- ✅ RPG có nhiều stat hiển thị nhiều nơi
- ✅ UI cần test logic (vd: tính giá item sau discount) mà không cần Unity Editor
- ❌ HUD đơn giản (1-2 field score, time) → overkill, dùng MonoBehaviour + ReactiveProperty
- ❌ Hyper-casual UI rất đơn giản → bỏ qua MVP

**Quy tắc:**
- View **không** import data class. View chỉ nhận lệnh `SetLabel(string)`, `SetFill(float)`.
- Presenter là pure C#, không kế thừa MonoBehaviour → test bằng NUnit dễ.
- Bind Presenter ↔ Model qua ReactiveProperty.

## 4. Singleton (an toàn)

**Vấn đề:** Cần truy cập GameManager từ mọi nơi nhưng `FindObjectOfType` chậm.

**Giải pháp:** `MonoSingleton<T>` - lazy, thread-safe, auto find/create, tránh ghost khi quit.

**Khi nào dùng:**
- ✅ Service không có interface rõ ràng, không có nhiều implementation
- ✅ Static context cần truy cập (vd: GameManager.Instance.Pause() từ pause button)
- ❌ **Trong template này ưu tiên ServiceLocator hơn Singleton** vì:
  - ServiceLocator có interface → dễ mock, dễ test
  - Singleton coupling cứng vào class cụ thể
  - Mỗi Singleton thêm = 1 điểm khó test

**Quy tắc:**
- Đừng tạo > 3 Singleton trong project. Quá nhiều = code spaghetti.
- Nếu lưỡng lự giữa Singleton và ServiceLocator → chọn ServiceLocator.

## 5. Async helpers (AsyncOp + Sequence)

**Vấn đề:** Coroutine dài dòng, khó chain, không return value, không xử lý exception tốt.

**Giải pháp:** `async/await` với `AsyncOp` wrapper. Sequence builder để chain animation tuần tự.

**Khi nào dùng:**
- ✅ Cutscene, tutorial sequence (chain nhiều bước có delay)
- ✅ UI animation fade in -> wait -> fade out
- ✅ Đợi điều kiện (vd: WaitUntil player chạm điểm)
- ✅ Load resources async, gọi API async
- ❌ Trong Update mỗi frame → vẫn dùng Update thường

**Lưu ý mobile:**
- `Task.Yield()` an toàn cho mobile vì Unity Task chạy trên main thread
- Đừng dùng `Task.Run` cho code đụng GameObject (crash)
- Nếu cần performance hơn → migrate sang **UniTask** (api gần như giống y chang)

## Bảng quyết định nhanh

| Tình huống | Pattern dùng |
|---|---|
| HP, score, coin đổi liên tục, nhiều UI hiển thị | ReactiveProperty |
| Spawn nhiều loại enemy có data khác nhau | Factory với ScriptableObject |
| Tạo object nhiều tham số tùy chọn | Builder |
| UI panel có logic phức tạp (inventory, shop) | MVP |
| HUD đơn giản 1-2 field | MonoBehaviour + ReactiveProperty (không cần MVP) |
| Truy cập GameManager từ mọi nơi | ServiceLocator (ưu tiên) hoặc MonoSingleton |
| Tutorial chain "highlight -> wait click -> next" | Sequence |
| Fade UI animation | AsyncOp.Tween |
| Network call, load asset | async/await + AsyncOp.WaitUntil |

## Module sẽ thêm sau (FSM / Command / Strategy)

Mỗi cái sẽ là 1 folder riêng trong `Scripts/Core/Patterns/` khi cần:

- **FSM**: enemy AI, game state, puzzle state. Tạo khi có game đầu tiên cần AI behavior.
- **Command**: undo/redo, replay. Tạo khi làm puzzle có undo hoặc turn-based RPG.
- **Strategy**: weapon behavior, AI difficulty. Tạo khi có game cần swap behavior runtime.

Pattern không có sẵn = ép mình suy nghĩ kỹ trước khi thêm. Đừng thêm pattern "phòng khi cần" - chỉ thêm khi đã thấy rõ use case.
