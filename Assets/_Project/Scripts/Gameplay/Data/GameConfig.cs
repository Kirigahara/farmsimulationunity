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

        [Header("Timer")]
        [SerializeField] float _DelayStartGameplay = 0.5f;
        [SerializeField] float _DelaySpawnGuest = 0.5f;
        [SerializeField] float _ProductGrowDelay = 0.2f;
        [SerializeField] float _ProductGetTime = 0.2f;
        [SerializeField] float _ProductMoveTime = 0.5f;

        public static GuestData[] AllGuestData => ServiceLocator.Get<GameplayBootstrap>()._GameConfig._AllGuestData;
        public static LevelConfig[] LevelConfigs => ServiceLocator.Get<GameplayBootstrap>()._GameConfig._LevelConfigs;
        public static float ProductGrowDelay => ServiceLocator.Get<GameplayBootstrap>()._GameConfig._ProductGrowDelay;
        public static float ProductMoveTime => ServiceLocator.Get<GameplayBootstrap>()._GameConfig._ProductMoveTime;
        public static float ProductGetTime => ServiceLocator.Get<GameplayBootstrap>()._GameConfig._ProductGetTime;
        public static float DelayStartGameplay => ServiceLocator.Get<GameplayBootstrap>()._GameConfig._DelayStartGameplay;
        public static float DelaySpawnGuest => ServiceLocator.Get<GameplayBootstrap>()._GameConfig._DelaySpawnGuest;
    }
}
