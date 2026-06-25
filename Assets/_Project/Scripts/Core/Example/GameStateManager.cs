using System;
using UnityEngine;
using GameTemplate.Core.DI;
using GameTemplate.Core.Events;
using GameTemplate.Core.Logger;
using GameTemplate.Core.Mobile.Haptic;
using GameTemplate.Core.Patterns.Reactive;
using GameTemplate.Core.Patterns.Singleton;

namespace GameTemplate.Gameplay.Core
{
    /// <summary>
    /// GameStateManager - track session state của game đang chơi.
    /// 
    /// VÌ SAO DÙNG SINGLETON Ở ĐÂY (không phải ServiceLocator):
    ///   1. Cần truy cập từ static context (vd: Cheat Console gọi GameStateManager.Instance.AddScore())
    ///   2. Là singleton TRONG NHÀ - chỉ logic game này dùng, không ai khác inject mock
    ///   3. Lifecycle phụ thuộc Unity (Update tick timer, OnApplicationPause)
    ///   4. KHÔNG có interface vì không cần swap implementation
    /// 
    /// Nếu cần test riêng -> dùng ServiceLocator + interface. Nhưng game state typical
    /// không cần test phức tạp nên Singleton là OK.
    /// 
    /// Triết lý: dùng Singleton SỐ ÍT, có ý đồ rõ ràng. Đừng abuse.
    /// </summary>
    public class GameStateManager : MonoSingleton<GameStateManager>
    {
        // ===== Reactive state - UI subscribe =====
        public ReactiveProperty<int> Score { get; } = new ReactiveProperty<int>(0);
        public ReactiveProperty<int> Combo { get; } = new ReactiveProperty<int>(0);
        public ReactiveProperty<float> SessionTime { get; } = new ReactiveProperty<float>(0f);
        public ReactiveProperty<bool> IsPaused { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<GameState> CurrentState { get; } = new ReactiveProperty<GameState>(GameState.Menu);

        // Flag cho cheat console
        public bool IsGodMode { get; set; }

        // Internal
        private float _sessionStartTime;
        private int _highestCombo;

        // ============================================================
        // LIFECYCLE
        // ============================================================
        protected override void Awake()
        {
            // QUAN TRỌNG: gọi base.Awake() để MonoSingleton handle DontDestroyOnLoad + duplicate check
            base.Awake();
            GameLog.Info(LogCategory.Gameplay, "GameStateManager ready.");
        }

        private void Update()
        {
            // Tick session time khi đang chơi
            if (CurrentState.Value == GameState.Playing && !IsPaused.Value)
            {
                SessionTime.Value = Time.realtimeSinceStartup - _sessionStartTime;
            }
        }

        private void OnApplicationPause(bool pause)
        {
            // Auto pause khi user tắt app (mobile)
            if (pause && CurrentState.Value == GameState.Playing)
            {
                SetPaused(true);
            }
        }

        // ============================================================
        // PUBLIC API
        // ============================================================

        public void StartNewSession()
        {
            Score.Value = 0;
            Combo.Value = 0;
            SessionTime.Value = 0;
            IsPaused.Value = false;
            IsGodMode = false;
            _highestCombo = 0;
            _sessionStartTime = Time.realtimeSinceStartup;
            CurrentState.Value = GameState.Playing;

            EventBus.Publish(new GameStartedEvent());
            GameLog.Info(LogCategory.Gameplay, "New session started.");
        }

        public void EndSession(bool isWin)
        {
            CurrentState.Value = GameState.GameOver;
            EventBus.Publish(new GameOverEvent
            {
                IsWin = isWin,
                Score = Score.Value
            });
            GameLog.Info(LogCategory.Gameplay,
                $"Session ended. Win:{isWin}, Score:{Score.Value}, Duration:{SessionTime.Value:F1}s, HighestCombo:{_highestCombo}");
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused.Value == paused) return;
            IsPaused.Value = paused;
            Time.timeScale = paused ? 0f : 1f;
            EventBus.Publish(new GamePausedEvent { IsPaused = paused });
        }

        public void TogglePause() => SetPaused(!IsPaused.Value);

        public void AddScore(int amount)
        {
            // GodMode cheat -> multiply x10 cho dễ test
            if (IsGodMode) amount *= 10;

            Score.Value += amount;
            Combo.Value++;
            if (Combo.Value > _highestCombo) _highestCombo = Combo.Value;

            // Haptic feedback khi combo cao
            if (Combo.Value % 10 == 0)
            {
                ServiceLocator.Get<IHapticService>().Play(HapticType.Medium);
            }
        }

        public void ResetCombo()
        {
            if (Combo.Value > 0)
            {
                Combo.Value = 0;
            }
        }

        public void GoToMenu()
        {
            CurrentState.Value = GameState.Menu;
            Time.timeScale = 1f;
        }
    }

    public enum GameState
    {
        Menu,
        Playing,
        GameOver
    }
}
