using GameTemplate.Core;
using GameTemplate.Core.DI;
using GameTemplate.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Sprite Config")]
public class SpriteConfig : ScriptableObject
{
    [SerializeField] Sprite _Icon_GlobalProfit;
    [SerializeField] Sprite _Icon_GuestAmount;

    [SerializeField] Sprite _Icon_Tomato;
    [SerializeField] Sprite _Icon_Corn;
    [SerializeField] Sprite _Icon_Apple;
    [SerializeField] Sprite _Icon_Grape;

    public static Sprite Icon_GlobalProfit => ServiceLocator.Get<GameplayBootstrap>()._SpriteConfig._Icon_GlobalProfit;
    public static Sprite Icon_GuestAmount => ServiceLocator.Get<GameplayBootstrap>()._SpriteConfig._Icon_GuestAmount;

    public static Sprite GetProductIcon(EnumManager.ProductType type)
    {
        switch (type)
        {
            default:
            case EnumManager.ProductType.Tomato: return ServiceLocator.Get<GameplayBootstrap>()._SpriteConfig._Icon_Tomato;
            case EnumManager.ProductType.Corn: return ServiceLocator.Get<GameplayBootstrap>()._SpriteConfig._Icon_Corn;
            case EnumManager.ProductType.Apple: return ServiceLocator.Get<GameplayBootstrap>()._SpriteConfig._Icon_Apple;
            case EnumManager.ProductType.Grape: return ServiceLocator.Get<GameplayBootstrap>()._SpriteConfig._Icon_Grape;
        }
    }
}

