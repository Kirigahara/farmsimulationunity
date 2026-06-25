# Architecture Diagrams

Đây là các diagram giải thích cách hệ thống hoạt động. Tất cả dùng [Mermaid](https://mermaid.js.org/) - GitHub tự render khi bạn xem file `.md` này.

**Để sửa diagram:** chỉ cần edit text trong code block, không cần tool ngoài.

---

## 1. Toàn cảnh kiến trúc 3 lớp

```mermaid
flowchart TB
    subgraph entry["🎬 Entry Point"]
        Bootstrap[Bootstrap Scene<br/>GameBootstrap.cs]
    end

    subgraph core["🔧 Core Framework - tái dùng nhiều game"]
        SL[ServiceLocator<br/>DI nhẹ]
        EB[EventBus<br/>Pub/Sub zero-alloc]
        SS[SaveService<br/>JSON + Atomic write]
        Loader[SceneLoader<br/>Async + Loading event]
        Audio[AudioManager<br/>Pool + Mixer]
        UI[UIManager<br/>Stack-based panel]
    end

    subgraph mobile["📱 Mobile Services"]
        Ads[AdsService<br/>Mediation switchable]
        IAP[IapService<br/>Purchase + Restore]
        Analytics[Analytics<br/>Multi-provider]
        Config[RemoteConfig<br/>A/B test ready]
        Loc[Localization<br/>CSV import]
        Haptic[HapticService<br/>iOS+Android]
    end

    subgraph gameplay["🎮 Gameplay - game-specific"]
        Player[Player Controller]
        Enemy[Enemies]
        Rules[Game Rules]
    end

    Bootstrap --> core
    Bootstrap --> mobile
    gameplay -.gọi qua interface.-> core
    gameplay -.gọi qua interface.-> mobile

    style entry fill:#fff4e6,stroke:#f59e0b
    style core fill:#e0e7ff,stroke:#6366f1
    style mobile fill:#e0f2fe,stroke:#0284c7
    style gameplay fill:#fce7f3,stroke:#ec4899
```

---

## 2. Bootstrap startup sequence (chi tiết)

Đây là thứ tự việc khi game khởi động. Cực kỳ quan trọng để debug "tại sao service X null":

```mermaid
sequenceDiagram
    participant U as Unity
    participant GB as GameBootstrap
    participant MSB as MobileBootstrapper
    participant SL as ServiceLocator
    participant Mock as Mock Services
    participant Scene as SceneLoader

    U->>GB: Awake()
    Note over GB: [DefaultExecutionOrder(-1000)]<br/>Chạy trước mọi script khác

    GB->>GB: ConfigureMobile()<br/>(60fps, screenSleep off)
    GB->>SL: Register<ISaveService>
    GB->>SL: Register<ISceneLoader>
    GB->>SL: Register<IAudioService>
    GB->>SL: Register<UIManager>

    GB->>MSB: InitializeAsync()
    MSB->>MSB: DeviceInfoService<br/>(detect tier Low/Mid/High)
    MSB->>MSB: ApplyTierSettings()<br/>(QualitySettings.SetQualityLevel)
    MSB->>SL: Register Haptic, Localization
    MSB->>Mock: RemoteConfig.FetchAsync()
    Mock-->>MSB: defaults loaded
    MSB->>SL: Register IRemoteConfigService
    par Init song song
        MSB->>Mock: Analytics.Initialize()
    and
        MSB->>Mock: Ads.Initialize()
    and
        MSB->>Mock: IAP.Initialize()
    end
    Mock-->>MSB: All ready
    MSB-->>GB: Done

    GB->>Scene: LoadSceneAsync("MainMenu")
    Scene-->>U: MainMenu loaded
    Note over U: 🎮 Game ready to play
```

**Debug tip:** Nếu game đứng ở Bootstrap không load MainMenu → mở Console (bật ENABLE_GAME_LOG) → xem log đến chỗ nào thì dừng → biết service nào fail init.

---

## 3. Service Locator pattern

Cách các module tìm thấy nhau mà không reference trực tiếp:

```mermaid
flowchart LR
    subgraph register["Bootstrap (chạy 1 lần)"]
        B[GameBootstrap.Awake] --> R1[ServiceLocator.Register<br/>IAudioService → audioMgr]
        B --> R2[ServiceLocator.Register<br/>ISaveService → saveSvc]
    end

    subgraph use["Gameplay (chạy nhiều lần)"]
        PC[CoinPickup.OnTriggerEnter] --> G1[ServiceLocator.Get<br/>&lt;IAudioService&gt;]
        PG[PlayerProgress.Save] --> G2[ServiceLocator.Get<br/>&lt;ISaveService&gt;]

        G1 --> Audio[audioMgr.PlaySfx]
        G2 --> Save[saveSvc.SaveAsync]
    end

    register -.dictionary lookup.-> use

    style register fill:#e0e7ff,stroke:#6366f1
    style use fill:#fce7f3,stroke:#ec4899
```

**Lợi ích:**
- Module gameplay không biết AudioManager cụ thể nào, chỉ biết `IAudioService`
- Đổi implementation (vd `AudioManager` → `FMODAudioManager`) → gameplay code không sửa
- Test dễ: register mock service trong unit test

---

## 4. Event Bus - giao tiếp giữa modules

Đây là cách module không phụ thuộc trực tiếp nhau:

```mermaid
flowchart TB
    subgraph publishers["📤 Publishers"]
        P1[CoinPickup]
        P2[Enemy.OnDeath]
        P3[Player.Jump]
    end

    EB((EventBus<br/>static))

    subgraph subscribers["📥 Subscribers - không biết publisher"]
        S1[ScoreManager]
        S2[AudioReactor]
        S3[VFXSpawner]
        S4[UIPopup]
        S5[Analytics]
    end

    P1 -->|CoinCollectedEvent| EB
    P2 -->|EnemyKilledEvent| EB
    P3 -->|PlayerJumpedEvent| EB

    EB -->|CoinCollectedEvent| S1
    EB -->|CoinCollectedEvent| S2
    EB -->|EnemyKilledEvent| S1
    EB -->|EnemyKilledEvent| S3
    EB -->|EnemyKilledEvent| S4
    EB -->|EnemyKilledEvent| S5
    EB -->|PlayerJumpedEvent| S2

    style publishers fill:#fef3c7,stroke:#f59e0b
    style EB fill:#dbeafe,stroke:#3b82f6
    style subscribers fill:#dcfce7,stroke:#22c55e
```

**Quy tắc:**
- 1 event = 1 struct (zero GC)
- Publisher không biết ai subscribe
- Subscriber phải `Unsubscribe` trong `OnDisable` (xem Cookbook recipe #10)

---

## 5. MVP UI pattern (RPG inventory ví dụ)

```mermaid
flowchart LR
    subgraph m["📦 Model (Pure C#)"]
        Data[InventoryData<br/>ReactiveCollection&lt;Item&gt;]
    end

    subgraph p["🧠 Presenter (Pure C#)"]
        Logic[InventoryPresenter<br/>Subscribe Model<br/>Update View]
    end

    subgraph v["🎨 View (MonoBehaviour)"]
        View[InventoryView<br/>SetItemCount<br/>AddItemSlot<br/>RemoveItemSlot]
    end

    Data -->|OnAdd/OnRemove event| Logic
    Logic -->|gọi method| View
    View -->|user click button| Logic
    Logic -->|update Model| Data

    style m fill:#fef3c7,stroke:#f59e0b
    style p fill:#dbeafe,stroke:#3b82f6
    style v fill:#dcfce7,stroke:#22c55e
```

**Lợi ích:**
- **View**: chỉ biết UI component (Text, Image), không biết business logic
- **Presenter**: chứa logic, test được mà không cần Unity Editor
- **Model**: data thuần, không phụ thuộc Unity
- Designer thay UI khác hoàn toàn → Presenter không sửa

---

## 6. Factory + Pool lifecycle (spawn enemy)

```mermaid
sequenceDiagram
    participant Caller as EnemyManager
    participant Factory as PrefabFactory
    participant Pool as ObjectPool
    participant Enemy

    Note over Caller,Enemy: Lần spawn đầu tiên

    Caller->>Factory: Create("slime", pos)
    Factory->>Pool: Init pool (size 8)
    loop 8 lần
        Pool->>Enemy: Instantiate prefab
        Pool->>Enemy: SetActive(false)
    end
    Factory->>Pool: Get()
    Pool->>Enemy: SetActive(true)
    Pool-->>Factory: enemy instance
    Factory->>Enemy: Configure(slimeData)
    Factory-->>Caller: enemy

    Note over Caller,Enemy: Enemy chết

    Caller->>Factory: Return("slime", enemy)
    Factory->>Pool: Release(enemy)
    Pool->>Enemy: SetActive(false)
    Note over Pool: Enemy trong pool, sẵn sàng spawn lần sau

    Note over Caller,Enemy: Spawn tiếp - KHÔNG Instantiate mới!

    Caller->>Factory: Create("slime", pos2)
    Factory->>Pool: Get() (đã có sẵn)
    Pool->>Enemy: SetActive(true)
    Pool-->>Factory: enemy (reused)
    Factory->>Enemy: Configure(slimeData)
    Factory-->>Caller: enemy
```

**Vì sao mobile cần pattern này:**
- `Instantiate`/`Destroy` mỗi enemy = GC spike = frame drop
- Pool reuse object → 0 alloc trong gameplay loop

---

## 7. Reactive Property data flow

Cách HP/Score tự update lên UI mà không cần gọi `UpdateUI()`:

```mermaid
flowchart LR
    subgraph game["Gameplay code"]
        Damage[player.TakeDamage 20]
        Damage --> Hp[Hp.Value = 80]
    end

    subgraph reactive["ReactiveProperty&lt;int&gt; Hp"]
        Hp --> Check{Value changed?}
        Check -->|yes| Notify[Notify subscribers]
        Check -->|no| Skip[Skip]
    end

    subgraph ui["UI subscribers - tự động"]
        Notify --> S1[HpText.text = '80']
        Notify --> S2[HpBar.fillAmount = 0.8]
        Notify --> S3[ShakeCamera if low HP]
    end

    style game fill:#fef3c7,stroke:#f59e0b
    style reactive fill:#dbeafe,stroke:#3b82f6
    style ui fill:#dcfce7,stroke:#22c55e
```

---

## 8. Mobile services - Mock vs Real SDK

Cách template build được mà không cần import SDK:

```mermaid
flowchart TB
    Game[Gameplay code]
    Game -->|depends on| IAds[IAdsService<br/>interface]

    IAds -.implementation.-> Factory{AdsServiceFactory.Create<br/>kiểm tra define}

    Factory -->|UNITY_EDITOR| Mock[MockAdsService<br/>log + return success<br/>luôn 'ready']
    Factory -->|ADS_UNITY define| UA[UnityAdsService<br/>compile khi có SDK]
    Factory -->|ADS_ADMOB define| AM[AdMobAdsService<br/>compile khi có SDK]
    Factory -->|ADS_APPLOVIN define| AL[AppLovinAdsService<br/>compile khi có SDK]
    Factory -->|none defined| Mock

    style Game fill:#fce7f3,stroke:#ec4899
    style IAds fill:#dbeafe,stroke:#3b82f6
    style Mock fill:#dcfce7,stroke:#22c55e
    style UA fill:#fef3c7,stroke:#f59e0b
    style AM fill:#fef3c7,stroke:#f59e0b
    style AL fill:#fef3c7,stroke:#f59e0b
```

**Triết lý:** Code gameplay trước, gắn SDK cuối. Khi chưa có SDK → Mock chạy bình thường, gameplay test được hết.

---

## 9. Scene flow (game lifecycle)

```mermaid
stateDiagram-v2
    [*] --> Bootstrap : App start
    Bootstrap --> Bootstrap : Init services
    Bootstrap --> MainMenu : All ready

    MainMenu --> Gameplay : Player bấm Play
    Gameplay --> Pause : ESC / pause button
    Pause --> Gameplay : Resume
    Pause --> MainMenu : Quit to menu

    Gameplay --> GameOver : Win/Lose
    GameOver --> Gameplay : Retry
    GameOver --> MainMenu : Back to menu

    note right of Bootstrap
        Chạy 1 lần duy nhất
        Services persist qua DontDestroyOnLoad
    end note

    note right of Gameplay
        Có thể là Level 1, 2, 3...
        Mỗi level = 1 scene riêng
        hoặc dùng 1 scene + load level data
    end note
```

---

## 10. Folder structure ↔ Namespace mapping

```mermaid
flowchart LR
    subgraph folders["📁 Folders"]
        F1[Scripts/Core/]
        F2[Scripts/Core/Mobile/Ads/]
        F3[Scripts/Core/Patterns/Reactive/]
        F4[Scripts/Gameplay/]
        F5[Scripts/UI/]
        F6[Scripts/Editor/]
    end

    subgraph ns["📛 Namespaces"]
        N1[GameTemplate.Core.&lt;System&gt;]
        N2[GameTemplate.Core.Mobile.Ads]
        N3[GameTemplate.Core.Patterns.Reactive]
        N4[GameTemplate.Gameplay.&lt;Feature&gt;]
        N5[GameTemplate.UI]
        N6[GameTemplate.Editor]
    end

    subgraph asm["📦 Assembly Definitions"]
        A1[GameTemplate.Core.asmdef]
        A2[GameTemplate.Gameplay.asmdef]
        A3[GameTemplate.Editor.asmdef]
    end

    F1 --> N1 --> A1
    F2 --> N2 --> A1
    F3 --> N3 --> A1
    F4 --> N4 --> A2
    F5 --> N5 --> A2
    F6 --> N6 --> A3

    A2 -->|references| A1
    A3 -->|references| A1
    A3 -->|references| A2

    style folders fill:#fef3c7,stroke:#f59e0b
    style ns fill:#dbeafe,stroke:#3b82f6
    style asm fill:#dcfce7,stroke:#22c55e
```

**Lợi ích Assembly Definition:**
- Sửa code Gameplay → chỉ compile Gameplay, không compile lại Core → nhanh hơn
- Ép kỷ luật: Core không reference Gameplay được → đảm bảo Core reusable

---

## 11. Adaptive Quality tier detection

```mermaid
flowchart TB
    Start([Device boot]) --> Detect[DeviceInfoService.DetectTier]

    Detect --> CheckRAM{RAM ≥ 6GB<br/>+ VRAM ≥ 2GB<br/>+ 8 cores?}
    CheckRAM -->|yes| High[Tier: High<br/>60fps, MSAA 2x<br/>Shadows ON]
    CheckRAM -->|no| CheckMid{RAM ≥ 3GB<br/>+ VRAM ≥ 1GB<br/>+ 4 cores?}
    CheckMid -->|yes| Mid[Tier: Mid<br/>60fps, no MSAA<br/>Shadows hard only]
    CheckMid -->|no| Low[Tier: Low<br/>30fps, no shadows<br/>Half texture res]

    High --> Apply[ApplyTierSettings<br/>QualitySettings.SetQualityLevel]
    Mid --> Apply
    Low --> Apply

    style High fill:#dcfce7,stroke:#22c55e
    style Mid fill:#fef3c7,stroke:#f59e0b
    style Low fill:#fee2e2,stroke:#ef4444
```

User có thể override trong Settings menu nếu muốn.

---

## Tips xem diagram trên GitHub

1. Mở repo trên GitHub → click vào file `DIAGRAMS.md`
2. GitHub tự render Mermaid → diagram hiện như ảnh
3. Click vào diagram để xem full screen
4. Edit ngay trên web: click pencil icon → sửa text → preview tab xem trước

**Nếu diagram không render:**
- GitHub support Mermaid từ 2022 - đảm bảo bạn đang trên github.com (không phải Bitbucket/legacy server)
- Nếu dùng GitLab: cũng support, từ 13.3+
- Local: cài extension VSCode "Markdown Preview Mermaid Support"
