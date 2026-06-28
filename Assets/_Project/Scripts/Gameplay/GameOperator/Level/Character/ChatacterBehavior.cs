using GameTemplate.Core.Patterns.Factory;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class ChatacterBehavior : MonoBehaviour
    {
        public FactoryDataBase _DefaultData;
        protected CharacterStat2 _CharacterStat;

        public PathNode _CurrentPathNode;
        //protected PathNode _FinishNode;

        public void UpdatePosition(Vector3 position)
        {
            this.transform.position = position;
        }
        public void UpdateRotation(Quaternion rotation) 
        {
            this.transform.rotation = rotation;
        }
    }
}
