using GameTemplate.Core.DI;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    [CreateAssetMenu(menuName = "Data/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("All Guest Config")]
        [SerializeField] GuestData[] _AllGuestData;

        [Header("Level Data")]
        [SerializeField] LevelConfig[] _LevelConfigs;

        public static GuestData[] AllGuestData => ServiceLocator.Get<GameplayBootstrap>()._GameConfig._AllGuestData;
        public static LevelConfig[] LevelConfigs => ServiceLocator.Get<GameplayBootstrap>()._GameConfig._LevelConfigs;
    }
}
