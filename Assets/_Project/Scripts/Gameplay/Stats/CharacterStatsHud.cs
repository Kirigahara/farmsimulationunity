using UnityEngine;
using UnityEngine.UI;

namespace GameTemplate.Gameplay.Stats
{
    /// <summary>
    /// CharacterStatsHud - subscribe ReactiveProperty của CharacterStats để hiện HP bar, stats, level.
    /// 
    /// Đây là MonoBehaviour + Reactive trực tiếp (không cần MVP đầy đủ) vì:
    ///   - UI chỉ display, không có business logic phức tạp
    ///   - Không có interaction phức tạp (chỉ pause button)
    /// 
    /// Khi UI tăng độ phức tạp (vd: stat allocation panel với nhiều button +/-, 
    /// equipment slot drag-drop) thì nâng cấp lên MVP như SettingsPanel.
    /// </summary>
    public class CharacterStatsHud : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("CharacterController trong scene - sẽ tìm nếu null")]
        [SerializeField] private CharacterController _controller;

        [Header("HP")]
        [SerializeField] private Image _hpFillBar;     // Image type = Filled, fillAmount 0-1
        [SerializeField] private Text _hpText;          // "80/120"

        [Header("Stats")]
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _attackText;
        [SerializeField] private Text _defenseText;

        [Header("EXP")]
        [SerializeField] private Image _expFillBar;
        [SerializeField] private Text _expText;

        [Header("Level Up")]
        [SerializeField] private GameObject _levelUpEffect;

        private CharacterStats _stats;

        private void Start()
        {
            if (_controller == null) _controller = FindAnyObjectByType<CharacterController>();
            // Đợi CharacterController init xong (Start chạy sau hoặc cùng frame)
            if (_controller.Stats == null)
            {
                // Stats chưa ready - đợi 1 frame
                Invoke(nameof(BindToStats), 0.1f);
            }
            else BindToStats();
        }

        private void BindToStats()
        {
            _stats = _controller.Stats;
            if (_stats == null) return;

            // Subscribe reactive - SubscribeWithInit để gọi callback luôn với value hiện tại
            _stats.CurrentHp.SubscribeWithInit(_ => RefreshHpBar());
            _stats.MaxHp.SubscribeWithInit(_ => RefreshHpBar());
            _stats.Level.SubscribeWithInit(lvl => _levelText.text = $"Lv. {lvl}");
            _stats.Attack.SubscribeWithInit(atk => _attackText.text = $"ATK {atk}");
            _stats.Defense.SubscribeWithInit(def => _defenseText.text = $"DEF {def}");
            _stats.CurrentExp.SubscribeWithInit(_ => RefreshExpBar());

            _stats.OnLevelUp += OnLevelUp;
            _stats.OnDied += OnDied;
        }

        private void OnDisable()
        {
            if (_stats == null) return;
            _stats.CurrentHp.Unsubscribe(_ => RefreshHpBar());
            _stats.MaxHp.Unsubscribe(_ => RefreshHpBar());
            _stats.OnLevelUp -= OnLevelUp;
            _stats.OnDied -= OnDied;
            // Tương tự cho các stat khác - production code nên giữ reference lambda để unsubscribe đúng
        }

        // ===== HP =====
        private void RefreshHpBar()
        {
            int current = _stats.CurrentHp.Value;
            int max = _stats.MaxHp.Value;
            _hpFillBar.fillAmount = max > 0 ? (float)current / max : 0f;
            _hpText.text = $"{current}/{max}";

            // Đổi màu khi HP thấp
            _hpFillBar.color = current < max * 0.3f ? Color.red : Color.green;
        }

        // ===== EXP =====
        private void RefreshExpBar()
        {
            int currentExp = _stats.CurrentExp.Value;
            int expToNext = _stats.Saved.GetExpToNextLevel();
            _expFillBar.fillAmount = expToNext > 0 ? (float)currentExp / expToNext : 0f;
            _expText.text = $"{currentExp}/{expToNext}";
        }

        // ===== Events =====
        private void OnLevelUp()
        {
            if (_levelUpEffect != null)
            {
                _levelUpEffect.SetActive(true);
                Invoke(nameof(HideLevelUpEffect), 2f);
            }
        }

        private void HideLevelUpEffect() => _levelUpEffect.SetActive(false);

        private void OnDied()
        {
            // Trigger game over UI, death animation, etc.
            UnityEngine.Debug.Log("Player died!");
        }
    }
}
