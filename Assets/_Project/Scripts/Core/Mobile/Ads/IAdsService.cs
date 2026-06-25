using System;
using System.Threading.Tasks;

namespace GameTemplate.Core.Mobile.Ads
{
    public enum AdMediation { Mock, UnityAds, AdMob, AppLovin, IronSource }
    public enum AdResult { Success, Failed, Skipped, Closed, NotReady }

    /// <summary>
    /// Ads Service - đã tách interface để swap mediation runtime.
    ///
    /// Code gameplay CHỈ depend interface này, không bao giờ depend impl cụ thể.
    /// Đổi SDK = đổi register trong Bootstrap, gameplay code không sửa 1 dòng.
    /// </summary>
    public interface IAdsService
    {
        AdMediation CurrentMediation { get; }

        // === Bật/tắt toàn cục ===
        bool AdsEnabled { get; set; }      // tắt toàn bộ ads (vd: remove ads IAP)
        bool BannerEnabled { get; set; }   // tắt riêng banner
        bool InterstitialEnabled { get; set; }
        bool RewardedEnabled { get; set; } // KHÔNG NÊN tắt rewarded vì user chủ động xem

        // === Lifecycle ===
        Task<bool> InitializeAsync();
        bool IsInitialized { get; }

        // === Banner ===
        void ShowBanner(BannerPosition position = BannerPosition.Bottom);
        void HideBanner();
        bool IsBannerVisible { get; }

        // === Interstitial (full-screen quảng cáo giữa game) ===
        bool IsInterstitialReady();
        Task<AdResult> ShowInterstitialAsync(string placement = "default");

        // === Rewarded Video (user xem để nhận thưởng) ===
        bool IsRewardedReady();
        Task<AdResult> ShowRewardedAsync(string placement = "default");

        // === Events cho UI hoặc Analytics subscribe ===
        event Action<string> OnAdShown;      // placement
        event Action<string> OnAdClicked;    // placement
        event Action<string> OnRewardEarned; // placement
    }

    public enum BannerPosition { Top, Bottom }
}
