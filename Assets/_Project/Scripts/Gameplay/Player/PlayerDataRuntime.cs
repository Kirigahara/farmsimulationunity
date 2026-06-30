using GameTemplate.Core.Patterns.Reactive;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class PlayerDataRuntime
    {
        public int _Gem;
        public BigNumber _Gold;

        public ReactiveProperty<int> Gem = new ReactiveProperty<int>(0);
        public ReactiveProperty<BigNumber> Gold = new ReactiveProperty<BigNumber>();

        public PlayerDataRuntime(PlayerData origin)
        {
            Gem = new ReactiveProperty<int>(origin._Gem);
            Gold = new ReactiveProperty<BigNumber>(origin._Gold);
        }

        public void UpGem(int value) => Gem.Value += value;
        public void DownGem(int value) => Gem.Value -= value;
        public void UpGold(BigNumber value) => Gold.Value += value;
        public void DownGold(BigNumber value) => Gold.Value -= value;
    }
}
