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
    }
}
