# Mobile Services Integration Guide

Template cung cấp **7 mobile services** với pattern "interface + Mock + skeleton SDK". Code gameplay viết được ngay, SDK gắn sau khi gần ship.

## Triết lý: Code trước, SDK sau

Vấn đề kinh điển khi làm mobile game:
1. Team đang code gameplay, designer chưa quyết AdMob hay AppLovin
2. Import SDK quá sớm → bloat build, compile chậm, dev không có account test
3. Code rải `if (UnityAds != null)` khắp nơi → khó maintain

Giải pháp của template:

```
Code gameplay  -->  IAdsService (interface)
                         |
                         ├── MockAdsService (mặc định, không cần SDK)
                         ├── UnityAdsService (compile khi ADS_UNITY define)
                         ├── AdMobAdsService (compile khi ADS_ADMOB define)
                         └── AppLovinAdsService (compile khi ADS_APPLOVIN define)
```

**Quy tắc:** Không có define nào → MockAdsService chạy → game vẫn build và play được. SDK bỏ qua hoàn toàn.

## Các service có sẵn

| Service | Interface | Mock | SDK skeleton | Define symbol |
|---|---|---|---|---|
| Ads | `IAdsService` | ✅ | Unity Ads, AdMob, AppLovin | `ADS_UNITY` / `ADS_ADMOB` / `ADS_APPLOVIN` |
| IAP | `IIapService` | ✅ | Unity IAP, Google Play Billing | `IAP_UNITY` / `IAP_GOOGLE_PLAY` |
| Analytics | `IAnalyticsService` | ✅ | Firebase | `ANALYTICS_FIREBASE` |
| Remote Config | `IRemoteConfigService` | ✅ | Firebase | `REMOTE_CONFIG_FIREBASE` |
| Localization | `ILocalizationService` | n/a (sync, đọc SO) | n/a | n/a |
| Haptic | `IHapticService` | ✅ (log) | Native iOS/Android | n/a (auto detect platform) |
| Device Info | `IDeviceInfoService` | n/a | n/a | n/a |

## Cách dùng từ gameplay code

```csharp
using GameTemplate.Core.DI;
using GameTemplate.Core.Mobile.Ads;
using GameTemplate.Core.Mobile.Analytics;
using GameTemplate.Core.Mobile.Haptic;

public class GameOverScreen : MonoBehaviour
{
    public async void OnReviveButtonClicked()
    {
        var ads = ServiceLocator.Get<IAdsService>();
        var haptic = ServiceLocator.Get<IHapticService>();
        var analytics = ServiceLocator.Get<IAnalyticsService>();

        haptic.Play(HapticType.Selection);

        if (!ads.IsRewardedReady())
        {
            analytics.TrackEvent("revive_no_ad");
            return;
        }

        var result = await ads.ShowRewardedAsync("revive");
        if (result == AdResult.Success)
        {
            analytics.TrackEvent("revive_success");
            ReviveLevel();
        }
    }
}
```

Code này hoạt động:
- ✅ Trên Editor (MockAdsService log + return success)
- ✅ Trên build chưa import SDK (MockAdsService)
- ✅ Sau khi import Unity Ads SDK + define `ADS_UNITY` (UnityAdsService)
- ✅ Sau khi đổi sang AdMob (AdMobAdsService)

## Workflow tích hợp SDK thật (khi gần ship)

### Bước 1: Quyết định mediation/provider

Đến lúc gần ship hoặc khi có ad account, team họp quyết:
- Ads: AppLovin MAX (eCPM cao) hay AdMob (mainstream)
- IAP: Unity IAP (đa platform sẵn) hay native
- Analytics: Firebase (free, tốt) hay GameAnalytics

### Bước 2: Import SDK qua Package Manager

```
# Unity Ads (đã có sẵn nếu dùng Unity 2020+)
Window > Package Manager > Unity Registry > Advertisement Legacy

# Google AdMob
Tải google-mobile-ads-x.x.x.unitypackage từ developers.google.com/admob/unity
Import vào project

# AppLovin MAX
Tải MaxSdk.unitypackage từ dash.applovin.com
Import vào project

# Firebase
Tải firebase_unity_sdk_x.x.x.zip
Import FirebaseAnalytics.unitypackage + FirebaseRemoteConfig.unitypackage

# Unity IAP
Window > Package Manager > In App Purchasing
```

### Bước 3: Define symbol

`Edit > Project Settings > Player > Scripting Define Symbols`:

```
ENABLE_GAME_LOG;ADS_APPLOVIN;IAP_UNITY;ANALYTICS_FIREBASE;REMOTE_CONFIG_FIREBASE
```

(Cách nhau bằng dấu `;`, không có space)

### Bước 4: Fill code trong skeleton

Mở file `RealAdsServices.cs`, tìm class tương ứng (vd `AppLovinAdsService`), uncomment các TODO comment, fill implementation theo doc SDK.

Trong template skeleton đã có:
- Lifecycle method stubs
- Comment hướng dẫn API gọi gì
- Link doc SDK trong comment

### Bước 5: Test

1. Build dev với `ENABLE_GAME_LOG` define
2. Cài lên device thật
3. Xem log để verify:
   - `[Bootstrap] Mobile services ready.`
   - `[Ads] [AppLovin] Initialized.`
   - `[IAP] Initialized với N sản phẩm.`
4. Test từng path: show ad, buy product, change language

## Bật tắt Ads runtime

User mua "Remove Ads" hoặc dev muốn tắt:

```csharp
var ads = ServiceLocator.Get<IAdsService>();

ads.AdsEnabled = false;          // tắt toàn bộ
ads.BannerEnabled = false;       // chỉ tắt banner, giữ interstitial + rewarded
ads.InterstitialEnabled = false; // tắt interstitial
// Rewarded vẫn nên giữ vì user chủ động xem để nhận thưởng
```

State này KHÔNG tự persist - bạn lưu vào Save service:

```csharp
public class PlayerData : SaveDataBase
{
    public bool RemoveAdsPurchased;
}

// Khi load save:
ads.AdsEnabled = !playerData.RemoveAdsPurchased;
```

## Switch mediation runtime qua Remote Config

Trong `MobileServicesBootstrapper`, set `_useRemoteConfigForAds = true`. Sau đó:

1. Firebase Console → Remote Config
2. Tạo param `ads_mediation` với value `"AppLovin"` hoặc `"AdMob"`
3. A/B test: 50% user dùng AppLovin, 50% AdMob để so eCPM
4. Tăng % cho mediation thắng

Lưu ý: Cần build có cả 2 SDK + cả 2 define symbol để switch được. Chỉ chọn approach này khi đã có 2 mediation account.

## Localization workflow

1. Create `LocalizationTable` ScriptableObject: `Assets > Create > GameTemplate > Localization > Localization Table`
2. Add entries:
   - Key: `ui.play`
     - Vietnamese: "Chơi"
     - English: "Play"
3. Gán vào `MobileServicesBootstrapper._localizationTable`
4. Trong code:
   ```csharp
   var loc = ServiceLocator.Get<ILocalizationService>();
   playButton.text = loc.Get("ui.play");
   ```
5. Đổi ngôn ngữ:
   ```csharp
   loc.SetLanguage(GameLanguage.English);
   // OnLanguageChanged event fire -> UI refresh
   ```

Tip: Viết component `LocalizedText : MonoBehaviour` subscribe `OnLanguageChanged` để mọi text tự refresh khi đổi ngôn ngữ.

## Adaptive Quality

Trong Bootstrap đã tự gọi `device.ApplyTierSettings()`. Game tự set quality theo tier.

User có thể override trong Settings menu:

```csharp
QualitySettings.SetQualityLevel(2); // High
Application.targetFrameRate = 60;
```

Lưu setting vào PlayerPrefs để giữ qua session.

## Analytics - Adapter pattern giải thích

Khi tích hợp Firebase Analytics, có thể bạn sẽ thấy code SDK Firebase dùng `Parameter[]`:

```csharp
// Firebase Analytics API thực tế
FirebaseAnalytics.LogEvent("level_complete", new Parameter[] {
    new Parameter("level", 5),
    new Parameter("duration", 120f)
});
```

**Nhưng `IAnalyticsService` của template dùng `Dictionary<string, object>`:**

```csharp
void TrackEvent(string eventName, Dictionary<string, object> parameters);
```

### Vì sao Dictionary thay vì Parameter[]?

**Lý do 1: Abstract khỏi vendor.** `Parameter` là class của Firebase SDK (`Firebase.Analytics.Parameter`). Nếu interface dùng nó:
- Mọi file gameplay phải `using Firebase.Analytics` mới gọi được
- Không build được khi chưa import Firebase SDK → `MockAnalyticsService` không chạy trên Editor
- Stuck với Firebase, không swap được sang GameAnalytics/AppsFlyer

**Lý do 2: Multi-provider support.** Template hiện tại có `AnalyticsService` (composite) - gửi event tới nhiều provider cùng lúc:

```csharp
analytics.RegisterProvider(new FirebaseAnalyticsProvider());
analytics.RegisterProvider(new GameAnalyticsProvider());
analytics.RegisterProvider(new AppsFlyerProvider());

analytics.TrackEvent("level_complete", new Dictionary<string, object>
{
    ["level"] = 5,
    ["duration"] = 120f
});
// → cả 3 provider đều nhận, mỗi provider tự convert sang SDK riêng
```

Nếu interface dùng `Parameter[]`, GameAnalytics phải convert ngược về Dictionary để xử lý → kỳ cục.

**Lý do 3: Dễ dùng từ gameplay code.** Dictionary có C# inline syntax gọn:

```csharp
// Gọn, không cần using Firebase
analytics.TrackEvent("ad_shown", new Dictionary<string, object>
{
    ["placement"] = "level_complete",
    ["ad_type"] = "interstitial"
});
```

### Đây là Adapter pattern

Template define interface theo "ngôn ngữ chung" (Dictionary), mỗi provider **adapter** convert sang SDK riêng:

```
Gameplay code (vendor-agnostic)
       │
       │ TrackEvent("level", Dictionary)
       ▼
IAnalyticsService  ← "ngôn ngữ chung" Dictionary
       │
       ├──► FirebaseAdapter    → convert → FirebaseAnalytics.LogEvent(Parameter[])
       ├──► GameAnalyticsAdapter → convert → GameAnalytics.NewDesignEvent(...)
       └──► AppsFlyerAdapter   → convert → AppsFlyer.SendEvent(Dictionary)
```

### Code mẫu: FirebaseAnalyticsProvider

Khi tích hợp Firebase thật, tạo file `FirebaseAnalyticsProvider.cs` trong `Assets/_Project/Scripts/Core/Mobile/Analytics/`:

```csharp
#if ANALYTICS_FIREBASE
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Analytics;
using UnityEngine;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Mobile.Analytics
{
    /// <summary>
    /// Firebase Analytics adapter - convert Dictionary<string, object> → Parameter[].
    /// Đăng ký vào AnalyticsService composite, không gọi trực tiếp.
    /// </summary>
    public class FirebaseAnalyticsProvider : IAnalyticsProvider
    {
        public string Name => "Firebase";
        public bool IsInitialized { get; private set; }

        public async Task<bool> InitializeAsync()
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
            {
                GameLog.Error(LogCategory.Analytics,
                    $"[Firebase] Dependencies không OK: {status}");
                return false;
            }

            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            IsInitialized = true;
            GameLog.Info(LogCategory.Analytics, "[Firebase] Analytics ready.");
            return true;
        }

        public void SetUserId(string userId)
            => FirebaseAnalytics.SetUserId(userId);

        public void SetUserProperty(string key, string value)
            => FirebaseAnalytics.SetUserProperty(key, value);

        public void TrackEvent(string eventName)
            => FirebaseAnalytics.LogEvent(eventName);

        public void TrackEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                FirebaseAnalytics.LogEvent(eventName);
                return;
            }

            // ⭐ CHỖ MAGIC: Dictionary → Parameter[]
            var parameterArray = new Parameter[parameters.Count];
            int i = 0;
            foreach (var kv in parameters)
            {
                parameterArray[i++] = ToFirebaseParameter(kv.Key, kv.Value);
            }
            FirebaseAnalytics.LogEvent(eventName, parameterArray);
        }

        /// <summary>
        /// Convert object value → Firebase Parameter.
        /// Firebase chỉ accept: string, long, double. Các type khác phải convert.
        /// </summary>
        private static Parameter ToFirebaseParameter(string key, object value)
        {
            return value switch
            {
                string s => new Parameter(key, s),
                int i    => new Parameter(key, i),
                long l   => new Parameter(key, l),
                float f  => new Parameter(key, f),
                double d => new Parameter(key, d),
                bool b   => new Parameter(key, b ? 1 : 0),  // Firebase không có bool
                null     => new Parameter(key, ""),
                _        => new Parameter(key, value.ToString())  // fallback toString
            };
        }
    }
}
#endif
```

Đăng ký vào Bootstrap:

```csharp
#if ANALYTICS_FIREBASE
analyticsService.RegisterProvider(new FirebaseAnalyticsProvider());
#endif
```

### Tạo provider khác (GameAnalytics, AppsFlyer)

Cùng pattern - implement `IAnalyticsProvider` interface, convert Dictionary sang format SDK đó:

```csharp
#if ANALYTICS_GAMEANALYTICS
public class GameAnalyticsProvider : IAnalyticsProvider
{
    public void TrackEvent(string eventName, Dictionary<string, object> parameters)
    {
        // GameAnalytics dùng string format "Category:Type:Item:Action"
        // Hoặc dùng custom dimensions
        if (parameters == null)
        {
            GameAnalytics.NewDesignEvent(eventName);
            return;
        }

        // GameAnalytics chỉ nhận 1 value số → pick "value" key nếu có
        if (parameters.TryGetValue("value", out var value) && value is float f)
            GameAnalytics.NewDesignEvent(eventName, f);
        else
            GameAnalytics.NewDesignEvent(eventName);
    }
    // ... các method khác
}
#endif
```

### Quy tắc khi viết provider mới

1. **Implement `IAnalyticsProvider` interface** - đảm bảo có TrackEvent, SetUserId, InitializeAsync
2. **Wrap trong `#if ANALYTICS_XXX`** - không define symbol thì class không tồn tại
3. **Mỗi provider tự convert Dictionary → SDK format** - không leak SDK type ra ngoài
4. **Handle null/empty parameters** gracefully - không crash khi event không có param
5. **Log via GameLog** với category `LogCategory.Analytics`
6. **Đăng ký trong Bootstrap** sau khi composite `AnalyticsService` đã tạo

### Workflow tích hợp Firebase Analytics - 5 bước

Khi muốn switch từ Mock sang Firebase thật, làm theo thứ tự:

#### Bước 1: Cài SDK + config files

- Download Firebase Unity SDK từ [console.firebase.google.com](https://console.firebase.google.com)
- Import `FirebaseAnalytics.unitypackage` qua Assets → Import Package
- Đặt `google-services.json` vào `Assets/` (Android)
- Đặt `GoogleService-Info.plist` vào `Assets/` (iOS)

#### Bước 2: Bật define symbol

Menu **GameTemplate → Define Symbol Manager** → tick `ANALYTICS_FIREBASE`.

Sau khi tick, class `FirebaseAnalyticsProvider` (đang nằm trong `#if ANALYTICS_FIREBASE`) mới được compile.

#### Bước 3: Tạo file `FirebaseAnalyticsProvider.cs`

Copy code mẫu ở section trên, lưu vào:

```
Assets/_Project/Scripts/Core/Mobile/Analytics/FirebaseAnalyticsProvider.cs
```

File này:
- Implement `IAnalyticsProvider` interface (KHÔNG phải `IAnalyticsService`)
- Wrap trong `#if ANALYTICS_FIREBASE` để không break build khi chưa có SDK
- Convert `Dictionary<string, object>` → `Parameter[]` của Firebase

#### Bước 4: Đăng ký provider vào Bootstrap

Chỗ này **đã viết sẵn** trong `MobileServicesBootstrapper.cs`:

```csharp
// 5. Analytics
var analytics = new AnalyticsService();
analytics.RegisterProvider(new MockAnalyticsProvider());
#if ANALYTICS_FIREBASE
analytics.RegisterProvider(new FirebaseAnalyticsProvider());  // ← tự compile khi define on
#endif
ServiceLocator.Register<IAnalyticsService>(analytics);
```

Khi tick define ở Bước 2, dòng `RegisterProvider(new FirebaseAnalyticsProvider())` tự được compile và chạy lúc Bootstrap. **Không phải sửa Bootstrap.**

#### Bước 5: Verify trên device

1. Build dev với `ENABLE_GAME_LOG` + `ANALYTICS_FIREBASE` defines
2. Cài lên device Android/iOS thật (Editor không gửi được)
3. Mở Firebase Console → Analytics → **DebugView**
4. Gọi trong code:
   ```csharp
   analytics.TrackEvent("test_event", new Dictionary<string, object>
   {
       ["test"] = "hello"
   });
   ```
5. Sự kiện phải xuất hiện trong DebugView trong 1-2 phút

### Bảng "khi import Firebase phải sửa cái gì"

Đây là điểm mạnh của Adapter pattern - **chỉ thêm 1 file mới, không sửa file nào hết**:

| Component | Trước Firebase | Sau khi import Firebase | Có sửa? |
|---|---|---|---|
| `IAnalyticsService` (interface) | Dictionary API | Dictionary API | ❌ Không |
| `IAnalyticsProvider` (interface) | TrackEvent + Dictionary | TrackEvent + Dictionary | ❌ Không |
| `AnalyticsService` (composite) | Loop registered providers | Loop registered providers | ❌ Không |
| `MockAnalyticsProvider` | Log ra Console | Log ra Console | ❌ Không |
| Gameplay code (`TrackEvent` calls) | `Dictionary<string, object>` | `Dictionary<string, object>` | ❌ Không |
| `MobileServicesBootstrapper` | Có `#if ANALYTICS_FIREBASE` sẵn | Define được bật, dòng register tự chạy | ❌ Không (chỉ tick define) |
| **`FirebaseAnalyticsProvider.cs`** | **Chưa tồn tại** | **Tạo mới + implement IAnalyticsProvider** | ✅ **Tạo mới** |

### Khi nào CẦN sửa interface?

Trong một số trường hợp **buộc phải sửa** `IAnalyticsService` khi cần expose feature mới. Ví dụ:

**Firebase có `SetSessionTimeoutDuration(TimeSpan)`** - chỉnh thời gian timeout session. Nếu gameplay code cần gọi, phải:

**1. Thêm method vào interface:**
```csharp
public interface IAnalyticsService
{
    // ... methods cũ
    void SetSessionTimeout(System.TimeSpan duration);  // ← thêm
}
```

**2. Implement trong `AnalyticsService` composite (forward tới mọi provider):**
```csharp
public void SetSessionTimeout(TimeSpan duration)
{
    foreach (var p in _providers) p.SetSessionTimeout(duration);
}
```

**3. Mỗi provider implement (kể cả Mock):**
```csharp
// FirebaseProvider
public void SetSessionTimeout(TimeSpan duration)
    => FirebaseAnalytics.SetSessionTimeoutDuration(duration);

// MockProvider
public void SetSessionTimeout(TimeSpan duration) { /* no-op */ }

// GameAnalyticsProvider - nếu SDK không support feature này
public void SetSessionTimeout(TimeSpan duration) { /* no-op, log warning */ }
```

**Quy tắc:** chỉ thêm vào interface những thứ là **khái niệm chung** (có ý nghĩa với mọi analytics provider). Tránh thêm feature riêng của 1 SDK:

| Feature | Có thêm vào interface không? | Lý do |
|---|---|---|
| `SetUserId`, `TrackEvent`, `SetUserProperty` | ✅ Có | Khái niệm chung mọi analytics platform |
| `SetSessionTimeout` | ✅ Có | Hầu hết platform có khái niệm session |
| `SetAnalyticsCollectionEnabled` (Firebase only) | ⚠️ Cân nhắc | Một số provider không có khái niệm này |
| `LogECommercePurchaseWithParameters` (Firebase only) | ❌ Không | Quá specific - dùng `TrackPurchase` của template |
| `SetDefaultEventParameters` (Firebase only) | ❌ Không | Feature mới chỉ Firebase có |

Khi cần feature **chỉ Firebase có**, expose qua property `Provider` của FirebaseAnalyticsProvider rồi cast - đừng làm bẩn interface chung:

```csharp
// Cách đúng: cast provider khi cần feature đặc thù
if (analytics is AnalyticsService composite)
{
    var firebase = composite.GetProvider<FirebaseAnalyticsProvider>();
    firebase?.SetAnalyticsCollectionEnabled(false);  // Firebase-only API
}
```



- [ ] Mọi service đã có SDK thật (không còn Mock trên prod build)
- [ ] Define symbol đúng cho mỗi platform
- [ ] Firebase config file đặt đúng chỗ:
  - Android: `Assets/google-services.json`
  - iOS: `Assets/GoogleService-Info.plist`
- [ ] AdMob App ID đã set trong AndroidManifest.xml
- [ ] iOS info.plist có `SKAdNetworkItems` cho ads (Apple yêu cầu)
- [ ] IAP product ID match với Google Play Console + App Store Connect
- [ ] Restore Purchases button có trong Settings (iOS yêu cầu)
- [ ] Analytics test events đã xuất hiện trong Firebase DebugView
- [ ] Remote Config có defaults cho mọi key (offline vẫn chạy)
- [ ] Test Mock vẫn build được khi remove SDK define (đề phòng dev mới clone không có SDK)
