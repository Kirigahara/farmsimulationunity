using GameTemplate.Core.Patterns.Factory;
using GameTemplate.Gameplay;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Contruction Data")]
public class ContructionData : FactoryDataBase
{
    [SerializeField] double _Cost;
    [SerializeField] float _UpgrateTime;
    [SerializeField] float _GrowTime;
    [SerializeField] GameObject _ProductPrefab;
    [SerializeField] UpgradeLevelConfig[] _Levels;
   
    public override GameObject Prefab => _ProductPrefab;
    public double Cost => _Cost;
    public float UpgradeTime => _UpgrateTime;
    public float GrowTime => _GrowTime;

    public UpgradeLevelConfig[] Levels => _Levels;
}

