using GameTemplate.Core.Patterns.Factory;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Farmer Data")]
public class FarmerData : FactoryDataBase
{
    [SerializeField] float _MoveSpeed;
    [SerializeField] float _SpecialBuff;

    [SerializeField] GameObject _FarmerPrefab;

    public float MoveSpeed => _MoveSpeed;
    public float SpecialBuff => _SpecialBuff;
    public override GameObject Prefab => _FarmerPrefab;
}
