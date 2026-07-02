using BreakEternity;
using GameTemplate.Core.Events;
using GameTemplate.Core.Patterns.Async;
using GameTemplate.Core.UI.Buttons;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameTemplate.Gameplay
{
    public struct SetupUpgradePopup : IGameEvent
    {
        public string _ProductName;
        public int _Level;
        public double _Price;
        public double _SalePrice;
        public float _UpdateTime;
        public bool _IsMaxUpgrade;
        public Vector3 _ConstructionPosition;
        public Action ActionTriggerBtnLevelUp;
    }
    public struct ShowUpgradePopup : IGameEvent { }
    public struct HideUpgradePopup : IGameEvent { }
    public struct OnUpgradeSequence : IGameEvent
    {
        public float _UpgradeTime;
        public Action _CompleteUpgrade;
    }

    public class ConstructionUpgradeView : ConstructionBuildView
    {
        [SerializeField] protected TextMeshProUGUI _TxtLevel;
        [SerializeField] protected TextMeshProUGUI _TxtSalePrice;
        [SerializeField] protected TextMeshProUGUI _TxtUpgradeTime;
        [SerializeField] protected Slider _UpgradeTimeSlider;

        [SerializeField] EnhancedButton _BtnUpgrade;
        [SerializeField] GameObject _TxtMax;

        bool _OnUpgrade = false;

        public bool OnUpgrade => _OnUpgrade;

        protected override void Awake()
        {
            EventBus.Subscribe<SetupUpgradePopup>(SetUp);
            EventBus.Subscribe<ShowUpgradePopup>(ShowPopup);
            EventBus.Subscribe<HideUpgradePopup>(HidePopup);
            EventBus.Subscribe<OnUpgradeSequence>(MakeUpgrade);
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<SetupUpgradePopup>(SetUp);
            EventBus.Unsubscribe<ShowUpgradePopup>(ShowPopup);
            EventBus.Unsubscribe<HideUpgradePopup>(HidePopup);
            EventBus.Unsubscribe<OnUpgradeSequence>(MakeUpgrade);
        }

        public void ShowPopup(ShowUpgradePopup e)
        {
            this.gameObject.SetActive(true);
        }
        public void HidePopup(HideUpgradePopup e)
        {
            this.gameObject.SetActive(false);
        }
        public void SetUp(SetupUpgradePopup e)
        {
            _TxtLevel.text = $"Level {e._Level:D}";
            _TxtName.text = e._ProductName;

            _TxtSalePrice.text = BigDouble.fromDouble(e._SalePrice).ToString();

            _TxtUpgradeTime.text = $"{e._UpdateTime:F} s";

            _Price = BigDouble.fromDouble(e._Price);
            _TxtPrice.text = _Price.ToString();
            _TxtPrice.color =
                GameplayManager.PlayerDataService.PlayerData._Gold >= (_Price) ?
                Color.white : Color.red;

            _ActionUnlock = e.ActionTriggerBtnLevelUp;

            this.transform.position = e._ConstructionPosition + Vector3.up * GameConfig.ConstructionToPopup;

            _BtnUpgrade.gameObject.SetActive(!e._IsMaxUpgrade);
            _TxtMax.gameObject.SetActive(e._IsMaxUpgrade);
        }

        public void MakeUpgrade(OnUpgradeSequence e)
        {
            _BtnUpgrade.gameObject.SetActive(false);

            _OnUpgrade = true;
            _ = AsyncOp.Tween(
                0, 1, e._UpgradeTime,
                (value) =>
                {
                    _UpgradeTimeSlider.value = value;
                },
                () =>
                {
                    _OnUpgrade = false;
                    e._CompleteUpgrade.Invoke();
                });
        }
    }
}
