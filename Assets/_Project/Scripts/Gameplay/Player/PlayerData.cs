using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    // ---------------------------------------------------------------
    // Dữ liệu từng cây trong màn (do GD config, dynamic)
    // ---------------------------------------------------------------

    [Serializable]
    public class PlantSaveData
    {
        public string PlantId;      // "1", "2", "3"... do GD đánh số
        public int PlantLevel;      // level của cây
    }

    // ---------------------------------------------------------------
    // Dữ liệu upgrade quản lý từng cây (A)
    // ---------------------------------------------------------------

    [Serializable]
    public class PlantUpgradeSaveData
    {
        public string PlantId;      // khớp với PlantSaveData.PlantId
        public int UpgradeLevel;    // level upgrade quản lý của cây này
    }

    // ---------------------------------------------------------------
    // Dữ liệu 1 màn chơi — tạo khi người chơi vào màn lần đầu
    // ---------------------------------------------------------------

    [Serializable]
    public class LevelSaveData
    {
        public int LevelId;
        public int ActiveLevel;

        // Trạng thái từng cây trong màn (GD config bao nhiêu cây thì có bấy nhiêu)
        public List<PlantSaveData> Plants = new List<PlantSaveData>();

        // Upgrade quản lý
        public List<PlantUpgradeSaveData> PlantUpgradeLevels = new List<PlantUpgradeSaveData>(); // A
        public int GlobalPlantUpgradeLevel;  // B
        public int CustomerUpgradeLevel;     // C

        // ---------------------------------------------------------------
        // Helper — Plant
        // ---------------------------------------------------------------

        /// <summary>Lấy PlantSaveData theo PlantId. Trả về null nếu chưa có.</summary>
        public PlantSaveData GetPlant(string plantId)
        {
            foreach (var p in Plants)
                if (p.PlantId == plantId) return p;
            return null;
        }

        /// <summary>Thêm cây mới vào màn. Dùng khi init màn lần đầu.</summary>
        public PlantSaveData AddPlant(string plantId)
        {
            var plant = new PlantSaveData { PlantId = plantId, PlantLevel = 0 };
            Plants.Add(plant);
            return plant;
        }

        /// <summary>Lấy hoặc tạo mới PlantSaveData theo PlantId.</summary>
        public PlantSaveData GetOrAddPlant(string plantId)
        {
            return GetPlant(plantId) ?? AddPlant(plantId);
        }

        // ---------------------------------------------------------------
        // Helper — Plant Upgrade (A)
        // ---------------------------------------------------------------

        public int GetPlantUpgradeLevel(string plantId)
        {
            foreach (var p in PlantUpgradeLevels)
                if (p.PlantId == plantId) return p.UpgradeLevel;
            return 0;
        }

        public void SetPlantUpgradeLevel(string plantId, int level)
        {
            foreach (var p in PlantUpgradeLevels)
            {
                if (p.PlantId == plantId)
                {
                    p.UpgradeLevel = level;
                    return;
                }
            }
            PlantUpgradeLevels.Add(new PlantUpgradeSaveData
            {
                PlantId = plantId,
                UpgradeLevel = level
            });
        }
    }

    // ---------------------------------------------------------------
    // PlayerData — lưu toàn bộ tiến trình người chơi
    // ---------------------------------------------------------------

    [Serializable]
    public class PlayerData
    {
        public int _Gem;
        public BigNumber _Gold;
        public int _Level;  // màn hiện tại của người chơi

        /// <summary>
        /// Dữ liệu từng màn chơi. Tạo khi người chơi vào màn lần đầu.
        /// </summary>
        public List<LevelSaveData> Levels = new List<LevelSaveData>();

        // ---------------------------------------------------------------
        // Helper — Level
        // ---------------------------------------------------------------

        /// <summary>Lấy LevelSaveData theo LevelId. Trả về null nếu chưa chơi màn này.</summary>
        public LevelSaveData GetLevel(int levelId)
        {
            foreach (var l in Levels)
                if (l.LevelId == levelId) return l;
            return null;
        }

        /// <summary>
        /// Lấy hoặc tạo mới LevelSaveData.
        /// Gọi khi người chơi vào màn — tự tạo nếu chơi lần đầu.
        /// </summary>
        public LevelSaveData GetOrCreateLevel(int levelId)
        {
            var level = GetLevel(levelId);
            if (level != null) return level;

            level = new LevelSaveData { LevelId = levelId };
            Levels.Add(level);
            return level;
        }
    }
}
