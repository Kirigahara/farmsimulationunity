using GameTemplate.Core.Events;
using GameTemplate.Core.Patterns.Async;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class LevelUpgradePopup : MonoBehaviour
    {
        [SerializeField] GameObject _ItemPrefab;
        [SerializeField] Transform _GroupItem;

        private void Start()
        {
            SpawnItem();
        }

        async void SpawnItem()
        {
            for (int i = 0; i < GameConfig.CurrentLevelConfig.PlantUpgrades.Length; i++)
            {
                var updata = GameConfig.CurrentLevelConfig.PlantUpgrades[i];

                var id = updata.PlantId;
                var item = GameObject.Instantiate(_ItemPrefab, _GroupItem).GetComponent<LevelUpgradeItem>();
                int level = GameplayManager.CurrentLevelSaveData.GetPlantUpgradeLevel(id);
                double cost = updata.Levels[level].Cost;

                item.SetUp(
                    updata.PlantId,
                    GameConfig.CurrentLevelConfig.PlantUpgrades[i],
                    () =>
                    {
                        if (!GameplayManager.PlayerDataService.PlayerData._Gold.CanAfford(
                            BigNumber.FromRaw(cost)))
                            return;

                        GameplayManager.PlayerDataRuntime.DownGold(BigNumber.FromRaw(cost));

                        level++;
                        GameplayManager.CurrentLevelSaveData.SetPlantUpgradeLevel(id, level);
                    });
            }

            await AsyncOp.Delay(Time.deltaTime);

            var item_Gp = GameObject.Instantiate(_ItemPrefab, _GroupItem).GetComponent<LevelUpgradeItem>();
            int level_Gp = GameplayManager.CurrentLevelSaveData.GlobalPlantUpgradeLevel;
            double cost_Gp = GameConfig.CurrentLevelConfig.GlobalPlantUpgrade.Levels[level_Gp].Cost;

            item_Gp.SetUp(
                "GlobalProfit",
                GameConfig.CurrentLevelConfig.GlobalPlantUpgrade,
                () =>
                {
                    if (!GameplayManager.PlayerDataService.PlayerData._Gold.CanAfford(
                            BigNumber.FromRaw(cost_Gp)))
                        return;

                    GameplayManager.PlayerDataRuntime.DownGold(BigNumber.FromRaw(cost_Gp));
                    GameplayManager.CurrentLevelSaveData.GlobalPlantUpgradeLevel++;
                });

            await AsyncOp.Delay(Time.deltaTime);

            var item_Guest = GameObject.Instantiate(_ItemPrefab, _GroupItem).GetComponent<LevelUpgradeItem>();
            int level_Guest = GameplayManager.CurrentLevelSaveData.CustomerUpgradeLevel;
            double cost_Guest = GameConfig.CurrentLevelConfig.CustomerUpgrade.Levels[level_Guest].Cost;
            

            item_Guest.SetUp(
                "GuestAmount",
                GameConfig.CurrentLevelConfig.CustomerUpgrade,
                () =>
                {
                    if (!GameplayManager.PlayerDataService.PlayerData._Gold.CanAfford(
                            BigNumber.FromRaw(cost_Guest)))
                        return;

                    GameplayManager.PlayerDataRuntime.DownGold(BigNumber.FromRaw(cost_Guest));
                    GameplayManager.CurrentLevelSaveData.CustomerUpgradeLevel++;

                    EventBus.Publish(new EventOpenGuest());
                });
        }
    }
}
