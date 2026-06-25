using System;
using System.Threading.Tasks;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Mobile.Ads
{
    /// <summary>
    /// Mock implementation - dùng khi:
    ///   - Editor (mọi SDK ads không chạy trên Editor)
    ///   - Chưa import SDK nào
    ///   - Test gameplay không muốn xem ads thật
    ///
    /// Behavior:
    ///   - Banner: in log, không hiện UI gì
    ///   - Interstitial: log + delay 1s + return Success
    ///   - Rewarded: log + delay 2s + return Success (user "đã xem hết")
    ///   - Tất cả luôn "ready" và "thành công" để gameplay test được mọi path
    /// </summary>
    public class MockAdsService : IAdsService
    {
        public AdMediation CurrentMediation => AdMediation.Mock;
        public bool IsInitialized { get; private set; }
        public bool AdsEnabled { get; set; } = true;
        public bool BannerEnabled { get; set; } = true;
        public bool InterstitialEnabled { get; set; } = true;
        public bool RewardedEnabled { get; set; } = true;
        public bool IsBannerVisible { get; private set; }

        public event Action<string> OnAdShown;
        public event Action<string> OnAdClicked;
        public event Action<string> OnRewardEarned;

        public async Task<bool> InitializeAsync()
        {
            GameLog.Info(LogCategory.Ads, "[Mock] Initializing...");
            await Task.Delay(100);
            IsInitialized = true;
            GameLog.Info(LogCategory.Ads, "[Mock] Initialized.");
            return true;
        }

        public void ShowBanner(BannerPosition position = BannerPosition.Bottom)
        {
            if (!AdsEnabled || !BannerEnabled)
            {
                GameLog.Info(LogCategory.Ads, "[Mock] Banner blocked (disabled).");
                return;
            }
            IsBannerVisible = true;
            GameLog.Info(LogCategory.Ads, $"[Mock] Banner shown at {position}.");
        }

        public void HideBanner()
        {
            IsBannerVisible = false;
            GameLog.Info(LogCategory.Ads, "[Mock] Banner hidden.");
        }

        public bool IsInterstitialReady() => AdsEnabled && InterstitialEnabled && IsInitialized;

        public async Task<AdResult> ShowInterstitialAsync(string placement = "default")
        {
            if (!AdsEnabled || !InterstitialEnabled)
            {
                GameLog.Info(LogCategory.Ads, $"[Mock] Interstitial '{placement}' blocked.");
                return AdResult.Skipped;
            }

            GameLog.Info(LogCategory.Ads, $"[Mock] Interstitial '{placement}' showing...");
            await Task.Delay(1000); // giả lập thời gian xem
            OnAdShown?.Invoke(placement);
            GameLog.Info(LogCategory.Ads, $"[Mock] Interstitial '{placement}' closed.");
            return AdResult.Closed;
        }

        public bool IsRewardedReady() => AdsEnabled && RewardedEnabled && IsInitialized;

        public async Task<AdResult> ShowRewardedAsync(string placement = "default")
        {
            if (!AdsEnabled || !RewardedEnabled)
            {
                GameLog.Info(LogCategory.Ads, $"[Mock] Rewarded '{placement}' blocked.");
                return AdResult.Skipped;
            }

            GameLog.Info(LogCategory.Ads, $"[Mock] Rewarded '{placement}' showing...");
            await Task.Delay(2000); // giả lập video 30s = 2s mock
            OnAdShown?.Invoke(placement);
            OnRewardEarned?.Invoke(placement);
            GameLog.Info(LogCategory.Ads, $"[Mock] Rewarded '{placement}' completed.");
            return AdResult.Success;
        }
    }
}
