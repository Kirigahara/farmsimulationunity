using GameTemplate.Core.Patterns.Async;
using GameTemplate.Core.Patterns.Factory;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class ProductionController : MonoBehaviour, IConfigurable<ContructionData>
    {
        public string _Id;

        public void Configure(ContructionData data)
        {

        }

        public void MoveSequence(Transform ToPosition)
        {
            _ = AsyncOp.MoveTween(
                this.transform.position,
                ToPosition, GameConfig.ProductMoveTime,
                (pos) =>
                {
                    this.transform.position = pos;
                },
                () =>
                {
                    this.transform.parent = ToPosition;
                }, null);
        }

        public void EmptyParent() => this.transform.parent = null;
    }
}
