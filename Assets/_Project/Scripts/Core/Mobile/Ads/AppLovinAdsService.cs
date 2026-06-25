#if ADS_APPLOVIN
using System;
using System.Threading.Tasks;
using UnityEngine;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Mobile.Ads
{
    /// <summary>
    /// AppLovin MAX implementation.
    ///
    /// Setup checklist trước khi dùng:
    ///   1. Import AppLovin MAX Unity SDK (.unitypackage) từ dash.applovin.com
    ///   2. Menu AppLovin → Integration Manager → cài thêm mediation networks
    ///      (Meta, Google AdMob, UnityAds, IronSource... tuỳ bạn chọn)
    ///   3. Dán SDK Key vào AppLovin → Integration Manager (key lấy từ dashboard)
    ///   4. Tạo Ad Units trên AppLovin Dashboard (Interstitial, Rewarded, Banner)
    ///   5. Đổi 4 ID dưới đây sang ID thật từ dashboard
    ///   6. Define symbol ADS_APPLOVIN trong Player Settings
    ///   7. Build app, mở Mediation Debugger (5-finger tap) để verify
    ///
    /// Tài liệu chính thức:
    ///   https://developers.applovin.com/en/unity/overview/integration/
    /// </summary>
    public class AppLovinAdsService : IAdsService
    {
        // ========================================================================
        // ⚠️ ĐỔI 4 ID NÀY THEO PROJECT CỦA BẠN
        // Lấy từ AppLovin Dashboard → Applications → Ad Units
        // ========================================================================
        private const string AndroidInterstitialAdUnitId = "YOUR_ANDROID_INTERSTITIAL_AD_UNIT_ID";
        private const string IosInterstitialAdUnitId = "YOUR_IOS_INTERSTITIAL_AD_UNIT_ID";
        private const string AndroidRewardedAdUnitId = "YOUR_ANDROID_REWARDED_AD_UNIT_ID";
        private const string IosRewardedAdUnitId = "YOUR_IOS_REWARDED_AD_UNIT_ID";
        private const string AndroidBannerAdUnitId = "YOUR_ANDROID_BANNER_AD_UNIT_ID";
        private const string IosBannerAdUnitId = "YOUR_IOS_BANNER_AD_UNIT_ID";

        // Retry logic - khi load fail, đợi rồi load lại với exponential backoff
        private int _interstitialRetryAttempt;
        private int _rewardedRetryAttempt;
        private const int MaxRetryAttempt = 6; // exp backoff: 2,4,8,16,32,64 giây

        // ===== Interface state =====
        public AdMediation CurrentMediation => AdMediation.AppLovin;
        public bool IsInitialized { get; private set; }
        public bool AdsEnabled { get; set; } = true;
        public bool BannerEnabled { get; set; } = true;
        public bool InterstitialEnabled { get; set; } = true;
        public bool RewardedEnabled { get; set; } = true;
        public bool IsBannerVisible { get; private set; }

        public event Action<string> OnAdShown;
        public event Action<string> OnAdClicked;
        public event Action<string> OnRewardEarned;

        // TaskCompletionSource bridge callback → async
        private TaskCompletionSource<bool> _initTcs;
        private TaskCompletionSource<AdResult> _interstitialTcs;
        private TaskCompletionSource<AdResult> _rewardedTcs;
        private string _currentPlacement;
        private bool _rewardEarned; // flag để biết user có complete rewarded ad không

        // ===== Platform-specific Ad Unit ID =====
        private static string InterstitialAdUnitId =>
            Application.platform == RuntimePlatform.IPhonePlayer
                ? IosInterstitialAdUnitId
                : AndroidInterstitialAdUnitId;

        private static string RewardedAdUnitId =>
            Application.platform == RuntimePlatform.IPhonePlayer
                ? IosRewardedAdUnitId
                : AndroidRewardedAdUnitId;

        private static string BannerAdUnitId =>
            Application.platform == RuntimePlatform.IPhonePlayer
                ? IosBannerAdUnitId
                : AndroidBannerAdUnitId;

        // ========================================================================
        // INITIALIZE
        // ========================================================================
        public Task<bool> InitializeAsync()
        {
            if (IsInitialized) return Task.FromResult(true);

            _initTcs = new TaskCompletionSource<bool>();

            // Subscribe callback init xong
            MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;

            // MAX cần SDK Key - nhưng MaxSdk auto-load từ menu AppLovin > Integration Manager.
            // Nếu bạn paste key qua menu thì không cần set ở đây.
            // Hoặc set tay: MaxSdk.SetSdkKey("YOUR_SDK_KEY");

            MaxSdk.InitializeSdk();

            GameLog.Info(LogCategory.Ads, "[AppLovin] InitializeSdk called, waiting...");
            return _initTcs.Task;
        }

        private void OnSdkInitialized(MaxSdkBase.SdkConfiguration config)
        {
            IsInitialized = true;
            GameLog.Info(LogCategory.Ads, $"[AppLovin] SDK initialized. Country: {config.CountryCode}");

            // GDPR (EU) / CCPA (California) - phải hỏi consent trước khi show targeted ads
            // MAX có UI built-in (Consent Flow) hoặc bạn tự build:
            //   MaxSdk.SetHasUserConsent(true);
            //   MaxSdk.SetIsAgeRestrictedUser(false);
            //   MaxSdk.SetDoNotSell(false);
            // Có thể fetch quyết định từ Firebase Remote Config theo region.

            // Subscribe các event và pre-load ad
            SetupInterstitialCallbacks();
            SetupRewardedCallbacks();
            SetupBannerCallbacks();

            LoadInterstitial();
            LoadRewarded();
            CreateBanner();

            _initTcs?.TrySetResult(true);
        }

        // ========================================================================
        // INTERSTITIAL
        // ========================================================================
        private void SetupInterstitialCallbacks()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClicked;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
        }

        private void LoadInterstitial() => MaxSdk.LoadInterstitial(InterstitialAdUnitId);

        public bool IsInterstitialReady()
        {
            if (!AdsEnabled || !InterstitialEnabled || !IsInitialized) return false;
            return MaxSdk.IsInterstitialReady(InterstitialAdUnitId);
        }

        public Task<AdResult> ShowInterstitialAsync(string placement = "default")
        {
            if (!AdsEnabled || !InterstitialEnabled)
                return Task.FromResult(AdResult.Skipped);

            if (!IsInterstitialReady())
            {
                GameLog.Warning(LogCategory.Ads, $"[AppLovin] Interstitial '{placement}' not ready.");
                // Retry load để lần sau có ad
                LoadInterstitial();
                return Task.FromResult(AdResult.NotReady);
            }

            _interstitialTcs = new TaskCompletionSource<AdResult>();
            _currentPlacement = placement;
            MaxSdk.ShowInterstitial(InterstitialAdUnitId, placement);
            return _interstitialTcs.Task;
        }

        private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo info)
        {
            _interstitialRetryAttempt = 0;
            GameLog.Info(LogCategory.Ads, $"[AppLovin] Interstitial loaded ({info.NetworkName})");
        }

        private void OnInterstitialLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            _interstitialRetryAttempt++;
            // Exponential backoff: 2, 4, 8, 16... giây - không spam load khi không có fill
            var retryDelay = Math.Pow(2, Math.Min(MaxRetryAttempt, _interstitialRetryAttempt));
            GameLog.Warning(LogCategory.Ads,
                $"[AppLovin] Interstitial load failed (attempt {_interstitialRetryAttempt}): {error.Message}. " +
                $"Retry in {retryDelay}s.");

            // Schedule retry trên main thread (MAX callback có thể không trên main thread)
            new GameObject("MaxAdsRetry_Int").AddComponent<DelayedAction>()
                .Run((float)retryDelay, LoadInterstitial);
        }

        private void OnInterstitialDisplayed(string adUnitId, MaxSdkBase.AdInfo info)
        {
            OnAdShown?.Invoke(_currentPlacement);
        }

        private void OnInterstitialClicked(string adUnitId, MaxSdkBase.AdInfo info)
        {
            OnAdClicked?.Invoke(_currentPlacement);
        }

        private void OnInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo info)
        {
            _interstitialTcs?.TrySetResult(AdResult.Closed);
            // MAX không auto-reload sau hidden - phải load lần tiếp theo
            LoadInterstitial();
        }

        private void OnInterstitialDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info)
        {
            GameLog.Error(LogCategory.Ads, $"[AppLovin] Interstitial display failed: {error.Message}");
            _interstitialTcs?.TrySetResult(AdResult.Failed);
            LoadInterstitial();
        }

        // ========================================================================
        // REWARDED
        // ========================================================================
        private void SetupRewardedCallbacks()
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedClicked;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedReceivedReward;
        }

        private void LoadRewarded() => MaxSdk.LoadRewardedAd(RewardedAdUnitId);

        public bool IsRewardedReady()
        {
            if (!AdsEnabled || !RewardedEnabled || !IsInitialized) return false;
            return MaxSdk.IsRewardedAdReady(RewardedAdUnitId);
        }

        public Task<AdResult> ShowRewardedAsync(string placement = "default")
        {
            if (!AdsEnabled || !RewardedEnabled)
                return Task.FromResult(AdResult.Skipped);

            if (!IsRewardedReady())
            {
                GameLog.Warning(LogCategory.Ads, $"[AppLovin] Rewarded '{placement}' not ready.");
                LoadRewarded();
                return Task.FromResult(AdResult.NotReady);
            }

            _rewardedTcs = new TaskCompletionSource<AdResult>();
            _currentPlacement = placement;
            _rewardEarned = false; // reset flag trước khi show
            MaxSdk.ShowRewardedAd(RewardedAdUnitId, placement);
            return _rewardedTcs.Task;
        }

        private void OnRewardedLoaded(string adUnitId, MaxSdkBase.AdInfo info)
        {
            _rewardedRetryAttempt = 0;
            GameLog.Info(LogCategory.Ads, $"[AppLovin] Rewarded loaded ({info.NetworkName})");
        }

        private void OnRewardedLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            _rewardedRetryAttempt++;
            var retryDelay = Math.Pow(2, Math.Min(MaxRetryAttempt, _rewardedRetryAttempt));
            GameLog.Warning(LogCategory.Ads,
                $"[AppLovin] Rewarded load failed (attempt {_rewardedRetryAttempt}): {error.Message}. " +
                $"Retry in {retryDelay}s.");

            new GameObject("MaxAdsRetry_Rew").AddComponent<DelayedAction>()
                .Run((float)retryDelay, LoadRewarded);
        }

        private void OnRewardedDisplayed(string adUnitId, MaxSdkBase.AdInfo info)
        {
            OnAdShown?.Invoke(_currentPlacement);
        }

        private void OnRewardedClicked(string adUnitId, MaxSdkBase.AdInfo info)
        {
            OnAdClicked?.Invoke(_currentPlacement);
        }

        // Reward event fire TRƯỚC OnAdHidden, khi user xem hết video.
        // Đây là chỗ duy nhất biết user có nhận thưởng được không.
        private void OnRewardedReceivedReward(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo info)
        {
            _rewardEarned = true;
            OnRewardEarned?.Invoke(_currentPlacement);
            GameLog.Info(LogCategory.Ads, $"[AppLovin] Reward earned: {reward.Amount} {reward.Label}");
        }

        private void OnRewardedHidden(string adUnitId, MaxSdkBase.AdInfo info)
        {
            // Phân biệt user xem hết (Success) vs skip giữa chừng (Skipped)
            var result = _rewardEarned ? AdResult.Success : AdResult.Skipped;
            _rewardedTcs?.TrySetResult(result);
            LoadRewarded();
        }

        private void OnRewardedDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info)
        {
            GameLog.Error(LogCategory.Ads, $"[AppLovin] Rewarded display failed: {error.Message}");
            _rewardedTcs?.TrySetResult(AdResult.Failed);
            LoadRewarded();
        }

        // ========================================================================
        // BANNER
        // ========================================================================
        private void SetupBannerCallbacks()
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += (adUnitId, info) =>
                GameLog.Info(LogCategory.Ads, $"[AppLovin] Banner loaded ({info.NetworkName})");
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += (adUnitId, error) =>
                GameLog.Warning(LogCategory.Ads, $"[AppLovin] Banner load failed: {error.Message}");
            MaxSdkCallbacks.Banner.OnAdClickedEvent += (adUnitId, info) =>
                OnAdClicked?.Invoke("banner");
        }

        private void CreateBanner()
        {
            // Tạo banner ở position default - sẽ override khi ShowBanner gọi
            MaxSdk.CreateBanner(BannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);
            // Background color cho banner (optional - khi quảng cáo nhỏ hơn vùng banner)
            MaxSdk.SetBannerBackgroundColor(BannerAdUnitId, new Color(0, 0, 0, 0));
        }

        public void ShowBanner(BannerPosition position = BannerPosition.Bottom)
        {
            if (!AdsEnabled || !BannerEnabled || !IsInitialized)
            {
                GameLog.Info(LogCategory.Ads, "[AppLovin] Banner blocked.");
                return;
            }

            var maxPos = position == BannerPosition.Top
                ? MaxSdkBase.BannerPosition.TopCenter
                : MaxSdkBase.BannerPosition.BottomCenter;
            MaxSdk.UpdateBannerPosition(BannerAdUnitId, maxPos);
            MaxSdk.ShowBanner(BannerAdUnitId);
            IsBannerVisible = true;
            GameLog.Info(LogCategory.Ads, $"[AppLovin] Banner shown at {position}");
        }

        public void HideBanner()
        {
            MaxSdk.HideBanner(BannerAdUnitId);
            IsBannerVisible = false;
            GameLog.Info(LogCategory.Ads, "[AppLovin] Banner hidden.");
        }
    }

    /// <summary>
    /// Helper component để delay action (dùng cho retry load ads).
    /// Tự destroy GameObject sau khi xong.
    /// </summary>
    internal class DelayedAction : MonoBehaviour
    {
        public void Run(float delay, Action action)
        {
            StartCoroutine(RunCoroutine(delay, action));
        }

        private System.Collections.IEnumerator RunCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            try { action?.Invoke(); }
            finally { Destroy(gameObject); }
        }
    }
}
#endif
