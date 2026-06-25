// =============================================================================
// CHỖ NÀY LÀ TEMPLATE CHO REAL SDK INTEGRATION
// =============================================================================
// File này KHÔNG compile cho đến khi bạn:
//   1. Import SDK tương ứng (Unity Ads / AdMob / AppLovin)
//   2. Define symbol trong Player Settings > Scripting Define Symbols
//
// Cách bật:
//   - Unity Ads:    define `ADS_UNITY`  + import package "com.unity.ads"
//   - AdMob:        define `ADS_ADMOB`  + import Google Mobile Ads SDK
//   - AppLovin MAX: define `ADS_APPLOVIN` + import AppLovin MAX SDK
//
// Khi chưa import SDK và chưa define symbol, file này hoàn toàn bị compiler
// strip ra -> không lỗi "missing namespace", không lỗi "type not found".
//
// QUY TRÌNH TÍCH HỢP SDK THẬT:
//   1. Import SDK qua Package Manager hoặc .unitypackage
//   2. Player Settings -> Scripting Define Symbols: thêm ADS_<NAME>
//   3. Uncomment và fill code trong từng #if block
//   4. Trong GameBootstrap, đổi:
//        ServiceLocator.Register<IAdsService>(new MockAdsService());
//      thành:
//        ServiceLocator.Register<IAdsService>(new UnityAdsService());
//        (hoặc AdMobAdsService, AppLovinAdsService)
//   5. Build và test trên device thật
// =============================================================================

using System;
using System.Threading.Tasks;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Mobile.Ads
{
#if ADS_UNITY
    /// <summary>Unity Ads implementation - chỉ tồn tại khi define ADS_UNITY.</summary>
    public class UnityAdsService : IAdsService
    {
        // TODO: implement khi import Unity Ads SDK
        // using UnityEngine.Advertisements;
        //
        // private string _androidGameId = "YOUR_ANDROID_GAME_ID";
        // private string _iosGameId = "YOUR_IOS_GAME_ID";
        // private bool _testMode = false; // true khi development
        //
        // public async Task<bool> InitializeAsync()
        // {
        //     var gameId = Application.platform == RuntimePlatform.IPhonePlayer ? _iosGameId : _androidGameId;
        //     Advertisement.Initialize(gameId, _testMode, new InitListener(this));
        //     await AsyncOp.WaitUntil(() => IsInitialized, timeout: 10f);
        //     return IsInitialized;
        // }
        //
        // public async Task<AdResult> ShowRewardedAsync(string placement = "rewardedVideo")
        // {
        //     var listener = new ShowListener();
        //     Advertisement.Show(placement, listener);
        //     await AsyncOp.WaitUntil(() => listener.IsComplete);
        //     return listener.Result;
        // }
        //
        // ... (xem doc Unity Ads để biết các listener)

        public AdMediation CurrentMediation => AdMediation.UnityAds;
        public bool IsInitialized { get; private set; }
        public bool AdsEnabled { get; set; } = true;
        public bool BannerEnabled { get; set; } = true;
        public bool InterstitialEnabled { get; set; } = true;
        public bool RewardedEnabled { get; set; } = true;
        public bool IsBannerVisible { get; private set; }

        public event Action<string> OnAdShown;
        public event Action<string> OnAdClicked;
        public event Action<string> OnRewardEarned;

        public Task<bool> InitializeAsync() => throw new NotImplementedException("Fill code in #if ADS_UNITY block.");
        public void ShowBanner(BannerPosition position = BannerPosition.Bottom) => throw new NotImplementedException();
        public void HideBanner() => throw new NotImplementedException();
        public bool IsInterstitialReady() => false;
        public Task<AdResult> ShowInterstitialAsync(string placement = "default") => throw new NotImplementedException();
        public bool IsRewardedReady() => false;
        public Task<AdResult> ShowRewardedAsync(string placement = "default") => throw new NotImplementedException();
    }
#endif

#if ADS_ADMOB
    /// <summary>Google AdMob implementation - chỉ tồn tại khi define ADS_ADMOB.</summary>
    public class AdMobAdsService : IAdsService
    {
        // TODO: implement với GoogleMobileAds.Api
        // using GoogleMobileAds.Api;
        //
        // private BannerView _bannerView;
        // private InterstitialAd _interstitial;
        // private RewardedAd _rewarded;
        //
        // public async Task<bool> InitializeAsync()
        // {
        //     var tcs = new TaskCompletionSource<bool>();
        //     MobileAds.Initialize(initStatus => tcs.SetResult(true));
        //     await tcs.Task;
        //     LoadInterstitial();
        //     LoadRewarded();
        //     return true;
        // }
        //
        // ... (xem doc AdMob Unity plugin)

        public AdMediation CurrentMediation => AdMediation.AdMob;
        public bool IsInitialized { get; private set; }
        public bool AdsEnabled { get; set; } = true;
        public bool BannerEnabled { get; set; } = true;
        public bool InterstitialEnabled { get; set; } = true;
        public bool RewardedEnabled { get; set; } = true;
        public bool IsBannerVisible { get; private set; }

        public event Action<string> OnAdShown;
        public event Action<string> OnAdClicked;
        public event Action<string> OnRewardEarned;

        public Task<bool> InitializeAsync() => throw new NotImplementedException("Fill code in #if ADS_ADMOB block.");
        public void ShowBanner(BannerPosition position = BannerPosition.Bottom) => throw new NotImplementedException();
        public void HideBanner() => throw new NotImplementedException();
        public bool IsInterstitialReady() => false;
        public Task<AdResult> ShowInterstitialAsync(string placement = "default") => throw new NotImplementedException();
        public bool IsRewardedReady() => false;
        public Task<AdResult> ShowRewardedAsync(string placement = "default") => throw new NotImplementedException();
    }
#endif

    // AppLovin MAX implementation: tách ra file AppLovinAdsService.cs (cùng folder).
}
