# AppLovin MAX Setup Guide

Hướng dẫn step-by-step tích hợp AppLovin MAX vào template. Đọc kèm với code trong `AppLovinAdsService.cs`.

## Tổng quan

AppLovin MAX là mediation platform - 1 SDK kết nối với nhiều ad network (AdMob, Meta, Unity Ads, IronSource...). MAX tự chọn network nào trả tiền cao nhất cho mỗi impression.

**Lợi ích vs đi single network:**
- eCPM cao hơn 20-50% (waterfall + bidding)
- Fill rate tốt hơn ở mọi geo
- 1 SDK quản lý, không phải maintain nhiều

## Bước 1: Tạo tài khoản AppLovin

1. Đăng ký tại [dash.applovin.com](https://dash.applovin.com)
2. Verify email + thông tin payment
3. **Tạo Application** (Apps → Add Application)
   - Nhập tên app + chọn iOS/Android
   - Sau khi tạo, copy **SDK Key** (long string ~80 chars)

## Bước 2: Import AppLovin MAX SDK

**Cách 1 - Unity Package Manager** (recommend):
```
Window → Package Manager → + → Add package from git URL:
https://github.com/AppLovin/AppLovin-MAX-Unity-Plugin.git
```

**Cách 2 - .unitypackage:**
1. Tải MaxSdk.unitypackage từ [dashboard](https://dash.applovin.com) → Account → Documents → Unity SDK
2. Assets → Import Package → Custom Package → chọn file đã tải

## Bước 3: Setup SDK Key

Menu Unity: **AppLovin → Integration Manager** → tab "Settings":
- Paste SDK Key vào ô "AppLovin SDK Key"
- Bấm Save

Việc này tạo file `MaxSdk/Resources/AppLovinSettings.asset` chứa SDK key. SDK tự load lúc init, **không cần** `MaxSdk.SetSdkKey()` trong code.

## Bước 4: Cài Mediation Networks

Menu **AppLovin → Integration Manager** → tab "Networks":

Bật các network sau:
- ✅ **DT Exchange** (luôn bật, free)
- ✅ **Google AdMob** (cần Key Android, request cho team marketting nếu chưa thấy key)
- ✅ **InMobi** (luôn bật, free)
- ✅ **LiftOff** (luôn bật, free)
- ✅ **Mintegral** (luôn bật, free)
- ✅ **Pangle** (rewarded tốt)
- ✅ **UnityAds** (rewarded tốt)

Bấm "Install" từng network → SDK tự download. Sau cùng bấm "Install All Required Dependencies".

**Lưu ý:** Mỗi network cần đăng ký account riêng và link ID vào AppLovin Dashboard (Mediation → Networks → Add Network).

## Bước 5: Tạo Ad Units trên Dashboard

[dash.applovin.com](https://dash.applovin.com) → Apps → chọn app → **MAX → Ad Units → Create Ad Unit**

Tạo 3 ad unit cho mỗi platform:

| Ad Unit | Format | Use case |
|---|---|---|
| `interstitial_main` | Interstitial | Full-screen sau game over |
| `rewarded_main` | Rewarded | Cho user xem để nhận thưởng |
| `banner_main` | Banner | Banner đáy màn hình |

Sau khi tạo, copy **Ad Unit ID** (mỗi unit 1 ID khác nhau, ~16 chars).

## Bước 6: Update Ad Unit IDs trong code

Mở `Assets/_Project/Scripts/Core/Mobile/Ads/AppLovinAdsService.cs`, dòng 27-32:

```csharp
private const string AndroidInterstitialAdUnitId = "YOUR_ANDROID_INTERSTITIAL_AD_UNIT_ID";
private const string IosInterstitialAdUnitId = "YOUR_IOS_INTERSTITIAL_AD_UNIT_ID";
private const string AndroidRewardedAdUnitId = "YOUR_ANDROID_REWARDED_AD_UNIT_ID";
private const string IosRewardedAdUnitId = "YOUR_IOS_REWARDED_AD_UNIT_ID";
private const string AndroidBannerAdUnitId = "YOUR_ANDROID_BANNER_AD_UNIT_ID";
private const string IosBannerAdUnitId = "YOUR_IOS_BANNER_AD_UNIT_ID";
```

Paste 6 ID thật từ dashboard. **Lưu ý:** Android và iOS ID khác nhau, không thể dùng chung.

**Tip giữ secret:** Nếu repo public, đừng commit Ad Unit ID lên. Có thể chuyển sang đọc từ ScriptableObject (không commit) hoặc environment variable.

## Bước 7: Bật Define Symbol

Menu **GameTemplate → Define Symbol Manager** → tick `ADS_APPLOVIN`.

Sau khi tick, Unity recompile và `AppLovinAdsService.cs` mới được compile (do nằm trong `#if ADS_APPLOVIN`).

## Bước 8: Verify trên thiết bị

### A. Test trên Editor

Trên Editor, `AdsServiceFactory.Create()` luôn return `MockAdsService` (vì SDK ads không chạy trên Editor). Bạn sẽ thấy log:

```
[Mock] Initializing...
[Mock] Initialized.
```

Editor không test AppLovin được - phải build cho device.

### B. Test trên device thật

1. Build Android development build, install lên device
2. Xem log qua **Android Logcat** (Window → Analysis → Android Logcat)
3. Filter `[AppLovin]` để thấy log của service:
   ```
   [AppLovin] InitializeSdk called, waiting...
   [AppLovin] SDK initialized. Country: VN
   [AppLovin] Interstitial loaded (AppLovinExchange)
   [AppLovin] Rewarded loaded (Mintegral)
   ```

### C. Mediation Debugger - tool cực hữu ích

Trong build, dùng **5-finger tap** vào màn hình bất kỳ → mở MAX Mediation Debugger:
- Xem network nào đang load được, network nào fail
- Test ad từng network riêng lẻ
- Verify config eCPM
- Check missing Android manifest entries

Hoặc trigger qua code khi cần:
```csharp
MaxSdk.ShowMediationDebugger();
```

Khi launch product cuối cùng, **tắt** trigger 5-finger để user không vô tình bật.

## Bước 9: GDPR / CCPA Consent

EU users (GDPR) và California users (CCPA) yêu cầu consent trước khi show ads target. AppLovin có Consent Flow built-in:

**Trong Inspector của `MaxSdk` setup:**
- Bật "Show Consent Flow" trong AppLovin → Integration Manager
- Setup URL Privacy Policy của bạn
- MAX tự hiện popup consent lần đầu app launch

**Hoặc set tay trong code:**
```csharp
// Trong AppLovinAdsService.OnSdkInitialized()
MaxSdk.SetHasUserConsent(true);     // GDPR
MaxSdk.SetIsAgeRestrictedUser(false);
MaxSdk.SetDoNotSell(false);          // CCPA
```

Có thể fetch consent decision từ Firebase Remote Config theo region (vd: VN không cần GDPR thì auto skip flow).

## Bước 10: iOS - ATT (App Tracking Transparency)

iOS 14+ yêu cầu hỏi user xin permission tracking trước khi show ads cá nhân hoá. Phải add vào `Info.plist`:

```xml
<key>NSUserTrackingUsageDescription</key>
<string>This app uses tracking to show you more relevant ads.</string>
```

Trong Unity: **Project Settings → Player → iOS → Other Settings → Custom Plist Entries**.

Và gọi request trước khi init MAX:

```csharp
// Trên iOS, gọi trước MaxSdk.InitializeSdk()
#if UNITY_IOS && !UNITY_EDITOR
    // Cần package "com.unity.ads.ios-support" hoặc tự viết native plugin
    Unity.Advertisement.IosSupport.ATTrackingStatusBinding.RequestAuthorizationTracking();
#endif
```

## Cách dùng trong gameplay (giống mọi mediation khác)

Code gameplay **không thay đổi** dù dùng MAX hay Unity Ads hay Mock:

```csharp
var ads = ServiceLocator.Get<IAdsService>();

// Banner
ads.ShowBanner(BannerPosition.Bottom);

// Interstitial sau level
if (ads.IsInterstitialReady())
{
    var result = await ads.ShowInterstitialAsync("level_complete");
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

Đây là điểm mạnh template: code business viết 1 lần, swap SDK chỉ qua define symbol.

## Common issues & fixes

### "AppLovin namespace not found"
- Chưa import SDK qua Package Manager
- Hoặc chưa define `ADS_APPLOVIN`

### "No fill" trong Mediation Debugger
- Mediation networks chưa active (cần đăng ký + verify account từng network)
- Ad Unit ID sai (nhầm test vs production)
- App chưa được approve trên network (Meta, AdMob phải review 1-2 ngày)

### Interstitial show được nhưng Rewarded không
- Rewarded format đôi khi thiếu inventory - đợi 1-2 ngày cho fill rate tăng
- Hoặc Ad Unit ID rewarded gán nhầm thành interstitial

### App crash khi show ad
- Thiếu Android dependencies → mở **Assets → External Dependency Manager → Android Resolver → Force Resolve**
- iOS thiếu Pod → mở **Assets → External Dependency Manager → iOS Resolver → Install Cocoapods**

### eCPM thấp (< $0.50)
- Tier 3 market (như VN nội địa) eCPM rẻ là bình thường
- Bật thêm bidding networks: Meta Bidding, Unity Bidding
- Setup Waterfall trên dashboard, không chỉ "Auto-CPM"

## Best practices cho mobile game

1. **Interstitial frequency cap:** không show liên tục, ít nhất 60s giữa 2 lần
   ```csharp
   if (Time.realtimeSinceStartup - _lastInterstitialTime < 60f) return;
   ```

2. **Rewarded placement đa dạng:** revive, double reward, unlock skin, daily bonus → nhiều entry point user xem

3. **Banner ẩn trong gameplay:** chỉ show ở Main Menu, Pause, Shop. Trong gameplay banner che view → bad UX

4. **No ads cho new user 3 phút đầu:** giữ retention. Code:
   ```csharp
   var sessionTime = Time.realtimeSinceStartup;
   if (sessionTime < 180f) ads.InterstitialEnabled = false;
   ```

5. **Remove Ads chỉ tắt Banner + Interstitial, GIỮ Rewarded:** user chủ động xem rewarded để nhận thưởng

6. **Track ad metric sang Analytics:** mỗi `OnAdShown` log event để xem placement nào hiệu quả

```csharp
ads.OnAdShown += (placement) => {
    analytics.TrackEvent("ad_shown", new Dictionary<string, object> {
        ["placement"] = placement,
        ["mediation"] = ads.CurrentMediation.ToString()
    });
};
```

## Tham khảo

- [AppLovin MAX Unity docs](https://developers.applovin.com/en/unity/overview/integration/)
<!-- - [Mediation networks list](https://developers.applovin.com/en/max/mediation-networks/)
- [Best practices](https://developers.applovin.com/en/max/preparing-mediation-of-ad-networks/) -->
- File code: `Assets/_Project/Scripts/Core/Mobile/Ads/AppLovinAdsService.cs`
