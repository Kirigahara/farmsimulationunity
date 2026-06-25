using UnityEngine;
using UnityEngine.UI;

namespace GameTemplate.Gameplay.Core
{
    /// <summary>
    /// Gameplay HUD - hiện score, combo, time.
    /// Subscribe ReactiveProperty của GameStateManager singleton.
    /// 
    /// Đây là example đơn giản (không cần MVP đầy đủ) vì HUD chỉ display
    /// không có business logic. Khi UI đơn giản, MonoBehaviour + Reactive 
    /// là đủ - không phải mọi UI đều cần Model-View-Presenter.
    /// </summary>
    public class GameplayHud : MonoBehaviour
    {
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _comboText;
        [SerializeField] private Text _timeText;
        [SerializeField] private GameObject _pauseIndicator;

        private void OnEnable()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null) return;

            // Subscribe reactive properties
            gsm.Score.SubscribeWithInit(OnScoreChanged);
            gsm.Combo.SubscribeWithInit(OnComboChanged);
            gsm.SessionTime.SubscribeWithInit(OnTimeChanged);
            gsm.IsPaused.SubscribeWithInit(OnPauseChanged);
        }

        private void OnDisable()
        {
            // GameStateManager có thể đã destroy (khi quit) -> check
            if (!GameStateManager.HasInstance) return;
            var gsm = GameStateManager.Instance;

            gsm.Score.Unsubscribe(OnScoreChanged);
            gsm.Combo.Unsubscribe(OnComboChanged);
            gsm.SessionTime.Unsubscribe(OnTimeChanged);
            gsm.IsPaused.Unsubscribe(OnPauseChanged);
        }

        private void OnScoreChanged(int score) => _scoreText.text = $"Score: {score}";

        private void OnComboChanged(int combo)
        {
            if (combo <= 1)
            {
                _comboText.text = "";
            }
            else
            {
                _comboText.text = $"x{combo} COMBO!";
            }
        }

        private void OnTimeChanged(float time)
        {
            // Format MM:SS
            int min = (int)(time / 60f);
            int sec = (int)(time % 60f);
            _timeText.text = $"{min:00}:{sec:00}";
        }

        private void OnPauseChanged(bool paused)
        {
            _pauseIndicator.SetActive(paused);
        }

        // Wire button trong Inspector
        public void OnPauseButtonClicked()
        {
            GameStateManager.Instance.TogglePause();
        }
    }
}
