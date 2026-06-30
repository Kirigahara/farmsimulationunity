using GameTemplate.Core.DI;
using GameTemplate.Core.Patterns.Singleton;
using TMPro;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class GameplayManager : MonoSingleton<GameplayManager>
    {
        public LevelController _MainLevel;

        PlayerDataService _PlayerDataService;
        LevelConfig _CurrentLevelConfig;
        PlayerDataRuntime _PlayerDataRuntime;

        [Header("UI")]
        [SerializeField] TextMeshProUGUI _Txt_Gem;
        [SerializeField] TextMeshProUGUI _Txt_Gold;

        bool _BindOnly = true;

        public static LevelController MainLevel => Instance._MainLevel;
        public static PlayerDataService PlayerDataService => Instance._PlayerDataService;
        public static LevelConfig CurrentLevelConfig => Instance._CurrentLevelConfig;
        public static LevelSaveData CurrentLevelSaveData =>
            PlayerDataService.PlayerData.GetLevel(
                PlayerDataService.PlayerData._Level);
        public static PlayerDataRuntime PlayerDataRuntime => Instance._PlayerDataRuntime;

        protected override void Awake()
        {
            base.Awake();

            _PlayerDataService = ServiceLocator.Get<PlayerDataService>();

            _CurrentLevelConfig = GameConfig.LevelConfigs[
                _PlayerDataService.PlayerData._Level];

            // Tạo mới Data save cho level nếu trong level chưa có data về cây
            if (CurrentLevelSaveData.Plants.Count == 0)
            {
                CurrentLevelSaveData.Plants = new System.Collections.Generic.List<PlantSaveData>();
                CurrentLevelSaveData.PlantUpgradeLevels = new System.Collections.Generic.List<PlantUpgradeSaveData>();
                CurrentLevelSaveData.GlobalPlantUpgradeLevel = 0;
                CurrentLevelSaveData.CustomerUpgradeLevel = 0;

                for (int i = 0; i < _CurrentLevelConfig.ContructionDatas.Length; i++)
                {
                    CurrentLevelSaveData.Plants.Add(
                        new PlantSaveData
                        {
                            PlantId = _CurrentLevelConfig.ContructionDatas[i].Id,
                            PlantLevel = 0
                        });
                    CurrentLevelSaveData.PlantUpgradeLevels.Add(
                        new PlantUpgradeSaveData
                        {
                            PlantId = _CurrentLevelConfig.ContructionDatas[i].Id,
                            UpgradeLevel = 0
                        });
                }

                _ = PlayerDataService.SaveAsync();
            }

            _PlayerDataRuntime = new PlayerDataRuntime(_PlayerDataService.PlayerData);

            bind();

            _Txt_Gem.text = _PlayerDataService.PlayerData._Gem.ToString();
            _Txt_Gold.text = _PlayerDataService.PlayerData._Gold.ToString();

            _MainLevel =
                LevelController.CreateLevel(
                    _PlayerDataService.PlayerData._Level,
                    Vector3.zero);

            _BindOnly = false;
        }

        void bind()
        {
            _PlayerDataRuntime.Gem.SubscribeWithInit(
                (gem) =>
                {
                    _PlayerDataService.PlayerData._Gem = gem;
                    _Txt_Gem.text = gem.ToString();

                    if (_BindOnly == false)
                        _ = GameplayManager.PlayerDataService.SaveAsync();
                });

            _PlayerDataRuntime.Gold.SubscribeWithInit(
                (gold) =>
                {
                    _PlayerDataService.PlayerData._Gold = gold;
                    _Txt_Gold.text = gold.ToString();

                    if (_BindOnly == false)
                        _ = GameplayManager.PlayerDataService.SaveAsync();
                });
        }

        public static BigNumber GetFruitSalePrice(string id)
        {
            int level_g = GameplayManager.CurrentLevelSaveData.GlobalPlantUpgradeLevel;
            UpgradeLevelConfig config_g;
            GameplayManager.CurrentLevelConfig.GlobalPlantUpgrade.TryGetLevel(
                level_g, out config_g, out _);
            int value_g = config_g.Value;

            int level = GameplayManager.CurrentLevelSaveData.GetPlantUpgradeLevel(id);
            UpgradeLevelConfig config;
            PlantUpgradeConfig pconfig;
            GameplayManager.CurrentLevelConfig.TryGetPlantUpgrade(id, out pconfig);
            pconfig.TryGetLevel(level, out config, out _);
            int value = config.Value;

            int level_p = CurrentLevelSaveData.GetPlant(id).PlantLevel;
            ContructionData config_p;
            GameplayManager.CurrentLevelConfig.TryGetPlantConfig(id, out config_p);

            double Price;
            (_, Price, _) = config_p.GetLevelConfig(level_p);

            return BigNumber.FromRaw(Price * value * value_g);
        }

        [ContextMenu(nameof(Hack100000Gold))]
        public void Hack100000Gold()
        {
            PlayerDataRuntime.UpGold(BigNumber.FromRaw(100000));
        }
    }
}
