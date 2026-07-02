using BreakEternity;
using GameTemplate.Core.Events;
using System;
using TMPro;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public struct SetupUnlockPopup : IGameEvent 
    { 
        public string _ProductName;
        public double _Price;
        public Vector3 _ConstructionPosition;
        public Action ActionTriggerBtnUnlock;
    }
    public struct ShowUnlockPopup: IGameEvent{}
    public struct HideUnlockPopup: IGameEvent{}

    public class ConstructionBuildView : MonoBehaviour
    {
        [SerializeField] protected TextMeshProUGUI _TxtName;
        [SerializeField] protected TextMeshProUGUI _TxtPrice;

        protected BigDouble _Price;
        protected Action _ActionUnlock;

        protected virtual void Awake()
        {
            EventBus.Subscribe<SetupUnlockPopup>(SetUp);
            EventBus.Subscribe<ShowUnlockPopup>(ShowPopup);
            EventBus.Subscribe<HideUnlockPopup>(HidePopup);
        }

        protected void Start()
        {
            this.gameObject.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            EventBus.Unsubscribe<SetupUnlockPopup>(SetUp);
            EventBus.Unsubscribe<ShowUnlockPopup>(ShowPopup);
            EventBus.Unsubscribe<HideUnlockPopup>(HidePopup);
        }

        public void ShowPopup(ShowUnlockPopup e)
        {
            this.gameObject.SetActive(true);
        }
        public void HidePopup(HideUnlockPopup e)
        {
            this.gameObject.SetActive(false);
        }

        public void SetUp(SetupUnlockPopup e)
        {
            _TxtName.text = e._ProductName;
            _Price = BigDouble.fromDouble(e._Price);
            _TxtPrice.text = _Price.ToString();
            _TxtPrice.color =
                GameplayManager.PlayerDataService.PlayerData._Gold >= (_Price) ?
                Color.white : Color.red;
            _ActionUnlock = e.ActionTriggerBtnUnlock;

            this.transform.position = e._ConstructionPosition + Vector3.up * GameConfig.ConstructionToPopup;
        }

        public void TriggerBtnUnlock()
        {
            _ActionUnlock?.Invoke();
        }
    }
}
