using GameTemplate.Core.Patterns.Factory;
using GameTemplate.Gameplay;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Contruction Data")]
public class ContructionData : FactoryDataBase
{
    [SerializeField] string _ProductName;
    [SerializeField] double _Cost;
    [SerializeField] float _UpgrateTime;
    [SerializeField] float _GrowTime;
    [SerializeField] GameObject _ProductPrefab;
    [SerializeField] UpgradeLevelConfig[] _Levels;
   
    public override GameObject Prefab => _ProductPrefab;
    public string ProductName => _ProductName;
    public double Cost => _Cost;
    public float UpgradeTime => _UpgrateTime;
    public float GrowTime => _GrowTime;

    public UpgradeLevelConfig[] Levels => _Levels;

    public (double, double, bool) GetLevelConfig(int level)
    {
        //if (level + 1 == _Levels.Length)
        //    return (-1, -1);

        //return
        //    (Levels[level].Cost,
        //    Levels[level].Value);

        return (_Levels[level].Cost, _Levels[level].Value, (level + 1 == _Levels.Length));
    }
}

