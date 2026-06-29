using GameTemplate.Core.DI;
using GameTemplate.Core.Patterns.Singleton;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class GameplayManager : MonoSingleton<GameplayManager>
    {
        public LevelController _MainLevel;
        
        PlayerDataService _PlayerDataService;

        public static LevelController MainLevel => Instance._MainLevel;
        public static PlayerDataService PlayerDataService => Instance._PlayerDataService;
        public static LevelSaveData CurrentLevelSaveData =>
            PlayerDataService.PlayerData.GetLevel(
                PlayerDataService.PlayerData._Level);

        protected override void Awake()
        {
            base.Awake();

            _PlayerDataService = ServiceLocator.Get<PlayerDataService>();

            _MainLevel =
                LevelController.CreateLevel(
                    _PlayerDataService.PlayerData._Level,
                    Vector3.zero);
        }
    }
}
