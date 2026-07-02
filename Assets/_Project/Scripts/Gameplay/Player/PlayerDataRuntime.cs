using BreakEternity;
using GameTemplate.Core.Patterns.Reactive;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class PlayerDataRuntime
    {
        public int _Gem;
        public BigDouble _Gold;

        public ReactiveProperty<int> Gem = new ReactiveProperty<int>(0);
        public ReactiveProperty<BigDouble> Gold = new ReactiveProperty<BigDouble>();

        public PlayerDataRuntime(PlayerData origin)
        {
            Gem = new ReactiveProperty<int>(origin._Gem);
            Gold = new ReactiveProperty<BigDouble>(origin._Gold);
        }

        public void UpGem(int value) => Gem.Value += value;
        public void DownGem(int value) => Gem.Value -= value;
        public void UpGold(BigDouble value) => Gold.Value += value;
        public void DownGold(BigDouble value) => Gold.Value -= value;
    }
}
