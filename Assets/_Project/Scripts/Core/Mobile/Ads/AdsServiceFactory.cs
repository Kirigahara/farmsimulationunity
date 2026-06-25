using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Mobile.Ads
{
    /// <summary>
    /// Factory tự động chọn implementation theo:
    ///   1. Define symbol (compile-time)
    ///   2. Editor luôn dùng Mock (vì SDK ads không chạy trên Editor)
    ///   3. Có thể override bằng Remote Config khi cần A/B test mediation
    ///
    /// Trong Bootstrap chỉ cần:
    ///   var ads = AdsServiceFactory.Create();
    ///   await ads.InitializeAsync();
    ///   ServiceLocator.Register<IAdsService>(ads);
    /// </summary>
    public static class AdsServiceFactory
    {
        public static IAdsService Create(AdMediation? forceMediation = null)
        {
#if UNITY_EDITOR
            // Editor: luôn Mock vì SDK ads không chạy trên Editor
            GameLog.Info(LogCategory.Ads, "Editor detected, dùng MockAdsService.");
            return new MockAdsService();
#else
            // Build thật: chọn theo define symbol
            // forceMediation cho phép Remote Config override mediation runtime
            var target = forceMediation ?? GetCompiledMediation();

            switch (target)
            {
#if ADS_UNITY
                case AdMediation.UnityAds: return new UnityAdsService();
#endif
#if ADS_ADMOB
                case AdMediation.AdMob: return new AdMobAdsService();
#endif
#if ADS_APPLOVIN
                case AdMediation.AppLovin: return new AppLovinAdsService();
#endif
                default:
                    GameLog.Warning(LogCategory.Ads,
                        $"Không có SDK ads nào được compile (target={target}). Fallback Mock.");
                    return new MockAdsService();
            }
#endif
        }

        /// <summary>Trả về mediation đầu tiên được compile. Ưu tiên: AppLovin > AdMob > UnityAds.</summary>
        public static AdMediation GetCompiledMediation()
        {
#if ADS_APPLOVIN
            return AdMediation.AppLovin;
#elif ADS_ADMOB
            return AdMediation.AdMob;
#elif ADS_UNITY
            return AdMediation.UnityAds;
#else
            return AdMediation.Mock;
#endif
        }
    }
}
