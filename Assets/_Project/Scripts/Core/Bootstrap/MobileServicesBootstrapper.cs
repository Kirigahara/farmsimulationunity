using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameTemplate.Core.DI;
using GameTemplate.Core.Logger;
using GameTemplate.Core.Mobile.Ads;
using GameTemplate.Core.Mobile.IAP;
using GameTemplate.Core.Mobile.Analytics;
using GameTemplate.Core.Mobile.RemoteConfig;
using GameTemplate.Core.Mobile.Localization;
using GameTemplate.Core.Mobile.Haptic;
using GameTemplate.Core.Mobile.Device;
using GameTemplate.Core.UI.Buttons;
using GameTemplate.Core.Scheduling;

namespace GameTemplate.Core.Bootstrap
{
    /// <summary>
    /// Bootstrapper riêng cho mobile services. Tách khỏi GameBootstrap chính để code rõ ràng.
    /// GameBootstrap gọi MobileServicesBootstrapper.InitializeAsync() trong Awake.
    ///
    /// Flow:
    ///   1. Device detection -> apply quality
    ///   2. Remote Config fetch (đợi vì các service khác có thể cần config)
    ///   3. Init song song: Ads, IAP, Analytics, Haptic, Localization
    /// </summary>
    public class MobileServicesBootstrapper : MonoBehaviour
    {
        [Header("Localization")]
        [SerializeField] private LocalizationTable _localizationTable;

        [Header("UI - Button SFX Library (optional)")]
        [Tooltip("Asset map preset SFX cho EnhancedButton. Để null = chỉ Custom clip work.")]
        [SerializeField] private UIButtonSfxLibrary _uiButtonSfxLibrary;

        [Header("IAP Products")]
        [SerializeField] private List<ProductInfo> _iapProducts = new List<ProductInfo>();

        [Header("Remote Config Defaults")]
        [SerializeField] private bool _useRemoteConfigForAds = false;

        public bool IsReady { get; private set; }

        public async Task InitializeAsync()
        {
            GameLog.Info(LogCategory.Bootstrap, "Initializing mobile services...");

            // 1. Device detection & quality
            var device = new DeviceInfoService();
            device.ApplyTierSettings();
            ServiceLocator.Register<IDeviceInfoService>(device);

            // 2. Haptic (sync, không cần init network)
            var haptic = new HapticService();
            ServiceLocator.Register<IHapticService>(haptic);

            // 2b. UI Button SFX Library (sync, optional)
            if (_uiButtonSfxLibrary != null)
            {
                ServiceLocator.Register<IUIButtonSfxLibrary>(_uiButtonSfxLibrary);
            }

            // 2c. Daily Reset (sync) - check ngày mới cho daily reward/quest
            ServiceLocator.Register<IDailyResetService>(new DailyResetService());

            // 3. Localization (sync, đọc ScriptableObject)
            if (_localizationTable != null)
            {
                var loc = new LocalizationService(_localizationTable);
                ServiceLocator.Register<ILocalizationService>(loc);
            }

            // 4. Remote Config trước - vì Ads có thể đọc config để chọn mediation
            var remoteConfig = RemoteConfigServiceFactory.Create();
            SetRemoteConfigDefaults(remoteConfig);
            ServiceLocator.Register<IRemoteConfigService>(remoteConfig);
            await remoteConfig.FetchAsync();

            // 5. Analytics (init song song với Ads, IAP)
            var analytics = new AnalyticsService();
            analytics.RegisterProvider(new MockAnalyticsProvider());
#if ANALYTICS_FIREBASE
            analytics.RegisterProvider(new FirebaseAnalyticsProvider());
#endif
            ServiceLocator.Register<IAnalyticsService>(analytics);

            // 6. Ads (có thể đọc Remote Config để chọn mediation)
            AdMediation? overrideMediation = null;
            if (_useRemoteConfigForAds)
            {
                var mediationName = remoteConfig.GetString("ads_mediation", "");
                if (System.Enum.TryParse<AdMediation>(mediationName, out var m))
                    overrideMediation = m;
            }
            var ads = AdsServiceFactory.Create(overrideMediation);

            // 7. IAP
            var iap = IapServiceFactory.Create();

            // Init Ads + IAP + Analytics song song để đỡ tốn startup time
            await Task.WhenAll(
                analytics.InitializeAsync(),
                ads.InitializeAsync(),
                iap.InitializeAsync(_iapProducts)
            );

            ServiceLocator.Register<IAdsService>(ads);
            ServiceLocator.Register<IIapService>(iap);

            // Hook: track ad shown -> analytics
            ads.OnAdShown += (placement) =>
                analytics.TrackAdShown(placement, "interstitial_or_rewarded");

            // Hook: track purchase -> analytics
            iap.OnPurchased += (productId) =>
            {
                var product = iap.GetProduct(productId);
                if (product != null)
                    analytics.TrackPurchase(productId, "USD", 0f); // price thực lấy từ store
            };

            IsReady = true;
            GameLog.Info(LogCategory.Bootstrap, "Mobile services ready.");
        }

        private void SetRemoteConfigDefaults(IRemoteConfigService rc)
        {
            // Default values khi Remote Config chưa fetch xong / offline
            rc.SetDefaults(new Dictionary<string, object>
            {
                // Ads
                ["ads_enabled"] = true,
                ["ads_mediation"] = "Mock",
                ["interstitial_min_interval"] = 60, // giây giữa 2 interstitial

                // Gameplay
                ["coin_drop_multiplier"] = 1.0f,
                ["xp_multiplier"] = 1.0f,

                // Feature flags
                ["new_shop_enabled"] = false,
                ["force_update_version"] = 0,
            });
        }
    }
}
