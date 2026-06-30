using GameTemplate.Core;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    // ---------------------------------------------------------------
    // Struct config cho từng level nâng cấp
    // ---------------------------------------------------------------

    [System.Serializable]
    public struct UpgradeLevelConfig
    {
        [Tooltip("Chi phí để nâng lên level này.")]
        public double Cost;

        [Tooltip("Giá trị hiệu lực của level này (yield, multiplier, customer count...).")]
        public int Value;
    }

    [System.Serializable]
    public class UpgradeContext
    {
        public string _Name;
        public string _Description;

        public string GetDescription(float value) 
        {
            return string.Format(_Description, value); 
        }
    }

    // ---------------------------------------------------------------
    // A. Config nâng cấp riêng cho từng loại cây
    // ---------------------------------------------------------------

    [System.Serializable]
    public class PlantUpgradeConfig: UpgradeContext 
    {
        [Tooltip("Id phải khớp với ConstructionData.Id của cây tương ứng.")]
        public string PlantId;

        public EnumManager.ProductType PlantType;

        [Tooltip("Danh sách các level nâng cấp. Index 0 = level 1, index 1 = level 2...")]
        public UpgradeLevelConfig[] Levels;

        public int MaxLevel => Levels?.Length ?? 0;

        public bool TryGetLevel(int level, out UpgradeLevelConfig config, out bool isMax)
        {
            int index = level + 1;
            if (Levels != null && index >= 0 && index < Levels.Length)
            {
                config = Levels[index];
                isMax = index == Levels.Length;
                return true;
            }
            config = Levels[Levels.Length - 1];
            isMax = true;
            return false;
        }
    }

    // ---------------------------------------------------------------
    // B. Config nâng cấp áp dụng toàn bộ cây
    // ---------------------------------------------------------------

    [System.Serializable]
    public class GlobalPlantUpgradeConfig : UpgradeContext
    {
        [Tooltip("Danh sách các level nâng cấp toàn bộ cây.")]
        public UpgradeLevelConfig[] Levels;

        public int MaxLevel => Levels?.Length ?? 0;

        public bool TryGetLevel(int level, out UpgradeLevelConfig config, out bool isMax)
        {
            int index = level + 1;
            if (Levels != null && index >= 0 && index < Levels.Length)
            {
                config = Levels[index];
                isMax = index == Levels.Length;
                return true;
            }
            config = Levels[Levels.Length - 1];
            isMax = true;
            return false;
        }
    }

    // ---------------------------------------------------------------
    // C. Config nâng cấp số lượng khách hàng
    // ---------------------------------------------------------------

    [System.Serializable]
    public class CustomerUpgradeConfig : UpgradeContext
    {
        [Tooltip("Danh sách các level nâng cấp khách hàng.")]
        public UpgradeLevelConfig[] Levels;

        public int MaxLevel => Levels?.Length ?? 0;

        public bool TryGetLevel(int level, out UpgradeLevelConfig config, out bool isMax)
        {
            int index = level + 1;
            if (Levels != null && index >= 0 && index < Levels.Length)
            {
                config = Levels[index];
                isMax = index == Levels.Length;
                return true;
            }
            config = Levels[Levels.Length - 1];
            isMax = true;
            return false;
        }
    }

    // ---------------------------------------------------------------
    // ScriptableObject tổng — 1 asset cho toàn bộ config nâng cấp quản lý
    // ---------------------------------------------------------------

    [CreateAssetMenu(menuName = "Data/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Config của từng cây")]
        public ContructionData[] ContructionDatas; 

        [Header("A. Nâng cấp riêng từng loại cây")]
        [Tooltip("Mỗi phần tử tương ứng 1 loại cây, PlantId phải khớp ConstructionData.Id.")]
        public PlantUpgradeConfig[] PlantUpgrades;

        [Header("B. Nâng cấp toàn bộ cây")]
        public GlobalPlantUpgradeConfig GlobalPlantUpgrade;

        [Header("C. Nâng cấp khách hàng")]
        public CustomerUpgradeConfig CustomerUpgrade;

        [Header("Prefab")]
        public GameObject _PrefabLevel;

        /// <summary>Tìm config nâng cấp của cây theo Id.</summary>
        public bool TryGetPlantUpgrade(string plantId, out PlantUpgradeConfig config)
        {
            if (PlantUpgrades != null)
            {
                foreach (var p in PlantUpgrades)
                {
                    if (p.PlantId == plantId)
                    {
                        config = p;
                        return true;
                    }
                }
            }
            config = null;
            return false;
        }

        /// <summary>Tìm config của cây theo Id.</summary>
        public bool TryGetPlantConfig(string plantId, out ContructionData config)
        {
            if (ContructionDatas != null)
            {
                foreach (var p in ContructionDatas)
                {
                    if (p.Id == plantId)
                    {
                        config = p;
                        return true;
                    }
                }
            }
            config = null;
            return false;
        }
    }
}
