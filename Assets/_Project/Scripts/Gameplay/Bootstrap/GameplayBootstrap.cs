using GameTemplate.Core;
using GameTemplate.Core.DI;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    [DefaultExecutionOrder(-999)] // Chạy sau GameBootstrap (-1000) nhưng trước mọi thứ khác
    public class GameplayBootstrap : MonoBehaviour
    {
        public GameConfig _GameConfig;

        private void Awake()
        {
            ServiceLocator.Register(this);

            // Register các service thuộc Gameplay
            ServiceLocator.Register(new PlayerDataService());

            // Thêm service Gameplay khác vào đây
            // ServiceLocator.Register(new InventoryService());
            // ServiceLocator.Register(new QuestService());

            LoadingScene.OnPreload = async () =>
            {
                await ServiceLocator.Get<PlayerDataService>().CheckPlayerData();
            };
        }
    }
}