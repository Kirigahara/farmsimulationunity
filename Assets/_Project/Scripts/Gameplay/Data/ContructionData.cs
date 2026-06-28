using GameTemplate.Core.Patterns.Factory;
using GameTemplate.Gameplay;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Contruction Data")]
public class ContructionData : FactoryDataBase
{
    [SerializeField] double _Cost;

    [SerializeField] GameObject _ProductPrefab;
    [SerializeField] UpgradeLevelConfig[] _Levels;
   
    public override GameObject Prefab => _ProductPrefab;

    public UpgradeLevelConfig[] Levels => _Levels;
}

