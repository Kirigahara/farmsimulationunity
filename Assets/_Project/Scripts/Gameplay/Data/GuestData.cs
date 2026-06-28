using GameTemplate.Core.Patterns.Factory;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Guest Data")]
public class GuestData : FactoryDataBase
{
    [SerializeField] float _MoveSpeed;
    [SerializeField] float _SpecialBuff;

    [SerializeField] GameObject _GuestPrefab;

    public float MoveSpeed => _MoveSpeed;
    public float SpecialBuff => _SpecialBuff;
    public override GameObject Prefab => _GuestPrefab;
}
