using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Mobile.IAP
{
    public enum ProductType { Consumable, NonConsumable, Subscription }
    public enum PurchaseResult { Success, Failed, Cancelled, AlreadyOwned, NotAvailable }

    [Serializable]
    public class ProductInfo
    {
        public string ProductId;          // ID match với store config (Google Play / App Store)
        public ProductType Type;
        public string LocalizedPrice;     // "$0.99" hoặc "29.000₫"
        public string LocalizedTitle;
        public string LocalizedDescription;
        public bool IsOwned;              // cho non-consumable & subscription
    }

    /// <summary>
    /// IAP Service - in-app purchase.
    /// Code gameplay chỉ depend interface này. Đổi SDK (Unity IAP / Google Play Billing
    /// / Apple StoreKit) không cần sửa gameplay code.
    /// </summary>
    public interface IIapService
    {
        bool IsInitialized { get; }
        Task<bool> InitializeAsync(IEnumerable<ProductInfo> productsToRegister);

        IReadOnlyList<ProductInfo> GetProducts();
        ProductInfo GetProduct(string productId);
        bool IsOwned(string productId);

        Task<PurchaseResult> PurchaseAsync(string productId);
        Task<bool> RestorePurchasesAsync(); // bắt buộc trên iOS để pass review

        event Action<string> OnPurchased;            // productId
        event Action<string, PurchaseResult> OnPurchaseFailed;
    }

    /// <summary>
    /// Mock IAP - test purchase flow không cần SDK + sandbox account.
    /// Mặc định mọi purchase thành công sau 1s.
    /// Override SimulatePurchaseResult để test path failed/cancelled.
    /// </summary>
    public class MockIapService : IIapService
    {
        private readonly Dictionary<string, ProductInfo> _products = new Dictionary<string, ProductInfo>();
        private readonly HashSet<string> _owned = new HashSet<string>();

        public bool IsInitialized { get; private set; }

        /// <summary>Set để giả lập purchase fail/cancel khi test UI error path.</summary>
        public PurchaseResult SimulatePurchaseResult { get; set; } = PurchaseResult.Success;

        public event Action<string> OnPurchased;
        public event Action<string, PurchaseResult> OnPurchaseFailed;

        public async Task<bool> InitializeAsync(IEnumerable<ProductInfo> productsToRegister)
        {
            GameLog.Info(LogCategory.IAP, "[Mock] Initializing IAP...");
            await Task.Delay(200);

            foreach (var p in productsToRegister)
            {
                // Mock giá theo region VN nếu chưa có
                if (string.IsNullOrEmpty(p.LocalizedPrice))
                    p.LocalizedPrice = "29.000₫";
                _products[p.ProductId] = p;
            }

            IsInitialized = true;
            GameLog.Info(LogCategory.IAP, $"[Mock] Initialized với {_products.Count} sản phẩm.");
            return true;
        }

        public IReadOnlyList<ProductInfo> GetProducts() => new List<ProductInfo>(_products.Values);

        public ProductInfo GetProduct(string productId)
        {
            _products.TryGetValue(productId, out var p);
            return p;
        }

        public bool IsOwned(string productId) => _owned.Contains(productId);

        public async Task<PurchaseResult> PurchaseAsync(string productId)
        {
            if (!_products.TryGetValue(productId, out var product))
            {
                GameLog.Error(LogCategory.IAP, $"[Mock] Product '{productId}' không tồn tại.");
                return PurchaseResult.NotAvailable;
            }

            GameLog.Info(LogCategory.IAP, $"[Mock] Buying '{productId}' ({product.LocalizedPrice})...");
            await Task.Delay(1000); // giả lập network

            if (SimulatePurchaseResult != PurchaseResult.Success)
            {
                OnPurchaseFailed?.Invoke(productId, SimulatePurchaseResult);
                return SimulatePurchaseResult;
            }

            if (product.Type != ProductType.Consumable)
            {
                _owned.Add(productId);
                product.IsOwned = true;
            }

            OnPurchased?.Invoke(productId);
            GameLog.Info(LogCategory.IAP, $"[Mock] Purchased '{productId}'.");
            return PurchaseResult.Success;
        }

        public async Task<bool> RestorePurchasesAsync()
        {
            GameLog.Info(LogCategory.IAP, "[Mock] Restore purchases...");
            await Task.Delay(500);
            // Mock không có gì để restore (không persist) - real SDK sẽ fetch từ store
            return true;
        }
    }

    /// <summary>Factory tự chọn impl theo define.</summary>
    public static class IapServiceFactory
    {
        public static IIapService Create()
        {
#if UNITY_EDITOR
            return new MockIapService();
#elif IAP_UNITY
            return new UnityIapService(); // implement khi import Unity IAP
#elif IAP_GOOGLE_PLAY
            return new GooglePlayIapService();
#else
            GameLog.Warning(LogCategory.IAP, "Không có IAP SDK nào được compile. Fallback Mock.");
            return new MockIapService();
#endif
        }
    }

    // Real implementations - chỉ compile khi define đúng symbol + import SDK
#if IAP_UNITY
    public class UnityIapService : IIapService
    {
        // TODO: implement với UnityEngine.Purchasing
        // Reference: https://docs.unity3d.com/Manual/UnityIAP.html
        public bool IsInitialized { get; private set; }
        public event Action<string> OnPurchased;
        public event Action<string, PurchaseResult> OnPurchaseFailed;
        public Task<bool> InitializeAsync(IEnumerable<ProductInfo> p) => throw new NotImplementedException();
        public IReadOnlyList<ProductInfo> GetProducts() => throw new NotImplementedException();
        public ProductInfo GetProduct(string id) => throw new NotImplementedException();
        public bool IsOwned(string id) => false;
        public Task<PurchaseResult> PurchaseAsync(string id) => throw new NotImplementedException();
        public Task<bool> RestorePurchasesAsync() => throw new NotImplementedException();
    }
#endif
}
