using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Core.Events
{
    /// <summary>
    /// Event Bus type-safe, zero-alloc cho mobile.
    /// Mỗi event là một struct implement IGameEvent để tránh GC.
    ///
    /// Cách dùng:
    ///   // Định nghĩa event
    ///   public struct PlayerDiedEvent : IGameEvent { public int Score; }
    ///
    ///   // Subscribe
    ///   EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
    ///
    ///   // Publish
    ///   EventBus.Publish(new PlayerDiedEvent { Score = 100 });
    ///
    ///   // Unsubscribe (BẮT BUỘC trong OnDestroy để tránh leak)
    ///   EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
    /// </summary>
    public static class EventBus
    {
        // Generic static class trick: mỗi T có 1 list riêng, không cần Dictionary<Type, List>.
        // Lookup O(1), không boxing, không GC khi publish.
        private static class EventChannel<T> where T : struct, IGameEvent
        {
            public static readonly List<Action<T>> Handlers = new List<Action<T>>(8);
        }

        public static void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            if (handler == null) return;
            EventChannel<T>.Handlers.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            if (handler == null) return;
            EventChannel<T>.Handlers.Remove(handler);
        }

        public static void Publish<T>(T evt) where T : struct, IGameEvent
        {
            var handlers = EventChannel<T>.Handlers;
            // Copy count để tránh lỗi khi handler tự unsubscribe trong lúc invoke
            int count = handlers.Count;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    handlers[i].Invoke(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Handler crash trên {typeof(T).Name}: {ex}");
                }
            }
        }

        public static void Clear<T>() where T : struct, IGameEvent
        {
            EventChannel<T>.Handlers.Clear();
        }
    }

    /// <summary>Marker interface cho game event. Phải là struct để tránh GC.</summary>
    public interface IGameEvent { }
}
