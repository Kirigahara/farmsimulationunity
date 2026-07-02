using BreakEternity;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameTemplate.Gameplay
{
    public class LevelUpgradeItem : MonoBehaviour
    {
        [SerializeField] Image _ImgIcon;
        [SerializeField] TextMeshProUGUI _Txt_Name;
        [SerializeField] TextMeshProUGUI _Txt_Decs;
        [SerializeField] TextMeshProUGUI _Txt_Price;

        [SerializeField] GameObject _BtnUpgrade;
        [SerializeField] GameObject _TxtMax;

        string _Id;
        UpgradeContext _Context;
        UpgradeLevelConfig[] _Levels;

        Action _ActionUpgrade;

        public void SetUp(
            string id,
            UpgradeContext context,
            Action ActionUpgrade)
        {
            _Id = id;
            _Context = context;

            _ActionUpgrade = ActionUpgrade;

            ReloadData();
        }

        private void OnEnable()
        {
            if (_Context == null) return;

            ReloadData();
        }

        void ReloadData()
        {
            _Txt_Name.text = _Context._Name;

            switch(_Id)
            {
                case "GlobalProfit":
                    {
                        int level = GameplayManager.CurrentLevelSaveData.GlobalPlantUpgradeLevel;
                        UpgradeLevelConfig config;
                        bool isMax = false;
                        GameplayManager.CurrentLevelConfig.GlobalPlantUpgrade.TryGetLevel(
                            level, out config, out isMax);

                        int value = config.Value;
                        _Txt_Decs.text = _Context.GetDescription(value);
                        _Txt_Price.text = BigDouble.fromDouble(config.Cost).ToString();

                        _ImgIcon.sprite = SpriteConfig.Icon_GlobalProfit;

                        _BtnUpgrade.gameObject.SetActive(!isMax);
                        _TxtMax.gameObject.SetActive(isMax);

                        _Txt_Price.color =
                            GameplayManager.PlayerDataService.PlayerData._Gold>=(
                                (config.Cost)) ? Color.white : Color.red;

                        break;
                    }
                case "GuestAmount":
                    {
                        int level = GameplayManager.CurrentLevelSaveData.CustomerUpgradeLevel;
                        UpgradeLevelConfig config;
                        bool isMax = false;
                        GameplayManager.CurrentLevelConfig.CustomerUpgrade.TryGetLevel(
                            level, out config, out isMax);

                        int value = config.Value;
                        _Txt_Decs.text = _Context.GetDescription(value);
                        _Txt_Price.text = BigDouble.fromDouble(config.Cost).ToString();

                        _ImgIcon.sprite = SpriteConfig.Icon_GuestAmount;

                        _BtnUpgrade.gameObject.SetActive(!isMax);
                        _TxtMax.gameObject.SetActive(isMax);

                        _Txt_Price.color =
                            GameplayManager.PlayerDataService.PlayerData._Gold>=(
                                (config.Cost)) ? Color.white : Color.red;

                        break;
                    }
                default:
                    {
                        int level = GameplayManager.CurrentLevelSaveData.GetPlantUpgradeLevel(_Id);
                        UpgradeLevelConfig config;
                        bool isMax = false;
                        PlantUpgradeConfig pconfig;
                        GameplayManager.CurrentLevelConfig.TryGetPlantUpgrade(_Id, out pconfig);
                        pconfig.TryGetLevel(level, out config, out isMax);

                        int value = config.Value;
                        _Txt_Decs.text = _Context.GetDescription(value);
                        _Txt_Price.text = BigDouble.fromDouble(config.Cost).ToString();

                        _ImgIcon.sprite = SpriteConfig.GetProductIcon(pconfig.PlantType);

                        _BtnUpgrade.gameObject.SetActive(!isMax);
                        _TxtMax.gameObject.SetActive(isMax);

                        _Txt_Price.color =
                            GameplayManager.PlayerDataService.PlayerData._Gold>=(
                                (config.Cost)) ? Color.white : Color.red;
                        break;
                    }
            }
        }

        public void TriggerBtnUpgrade()
        {
            _ActionUpgrade?.Invoke();
            ReloadData();
        }
    }
}
