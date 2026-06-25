using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Core.DI
{
    /// <summary>
    /// Service Locator nhẹ - đăng ký và lấy các service global mà không cần FindObjectOfType.
    /// Dùng cho mobile vì không tốn overhead của DI framework lớn như Zenject.
    ///
    /// Cách dùng:
    ///   ServiceLocator.Register<IAudioService>(audioManager);
    ///   var audio = ServiceLocator.Get<IAudioService>();
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>(16);

        /// <summary>Đăng ký một service. Throw nếu type đã tồn tại để tránh override im lặng.</summary>
        public static void Register<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service {type.Name} đã được register. Override.");
            }
            _services[type] = service;
        }

        /// <summary>Lấy service đã đăng ký. Throw nếu chưa register.</summary>
        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;

            throw new InvalidOperationException(
                $"[ServiceLocator] Service {typeof(T).Name} chưa được register. " +
                $"Hãy check Bootstrap scene.");
        }

        /// <summary>Thử lấy service, return false nếu chưa có (an toàn hơn Get).</summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var obj))
            {
                service = (T)obj;
                return true;
            }
            service = null;
            return false;
        }

        /// <summary>Hủy đăng ký một service (vd: khi shutdown).</summary>
        public static void Unregister<T>() where T : class
        {
            _services.Remove(typeof(T));
        }

        /// <summary>Clear toàn bộ - dùng khi reload domain hoặc test.</summary>
        public static void Clear()
        {
            _services.Clear();
        }
    }
}
