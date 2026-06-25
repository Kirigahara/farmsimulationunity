using UnityEngine;
using UnityEngine.UI;
using GameTemplate.Core.Patterns.Reactive;

namespace GameTemplate.Core.Patterns.MVP.Examples
{
    /// <summary>
    /// Ví dụ tham khảo: HP Bar dùng MVP + Reactive Property.
    /// Đây là code mẫu - khi làm game thật bạn copy pattern này.
    /// </summary>

    // === MODEL ===
    public class HpModel
    {
        public ReactiveProperty<int> Current { get; }
        public int Max { get; }

        public HpModel(int max)
        {
            Max = max;
            Current = new ReactiveProperty<int>(max);
        }

        public void TakeDamage(int amount)
            => Current.Value = Mathf.Max(0, Current.Value - amount);

        public void Heal(int amount)
            => Current.Value = Mathf.Min(Max, Current.Value + amount);
    }

    // === VIEW ===
    // View chỉ biết Unity UI component, KHÔNG biết Model.
    public class HpBarView : ViewBase
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private Text _label;

        // Presenter gọi method này, View chỉ render
        public void SetFill(float normalized) => _fillImage.fillAmount = normalized;
        public void SetLabel(string text) => _label.text = text;
    }

    // === PRESENTER ===
    public class HpBarPresenter : PresenterBase<HpBarView, HpModel>
    {
        public HpBarPresenter(HpBarView view, HpModel model) : base(view, model) { }

        protected override void OnInit()
        {
            // Subscribe Model -> update View khi data đổi
            Model.Current.SubscribeWithInit(OnHpChanged);
        }

        protected override void OnDispose()
        {
            Model.Current.Unsubscribe(OnHpChanged);
        }

        private void OnHpChanged(int hp)
        {
            View.SetFill((float)hp / Model.Max);
            View.SetLabel($"{hp}/{Model.Max}");
        }
    }

    /*
    Cách sử dụng:

    public class PlayerSetup : MonoBehaviour
    {
        [SerializeField] private HpBarView _hpBarView;
        private HpBarPresenter _hpPresenter;
        private HpModel _hpModel;

        private void Start()
        {
            _hpModel = new HpModel(max: 100);
            _hpPresenter = new HpBarPresenter(_hpBarView, _hpModel);
            _hpPresenter.Init();
        }

        private void OnDestroy() => _hpPresenter?.Dispose();

        // Khi player ăn đạn:
        public void TakeDamage(int dmg) => _hpModel.TakeDamage(dmg);
        // UI tự update qua ReactiveProperty + Presenter, không cần gọi UpdateUI()
    }
    */
}
