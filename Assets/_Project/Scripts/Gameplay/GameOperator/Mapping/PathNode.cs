using System;
using System.Collections.Generic;
using UnityEngine;
using static GameTemplate.Core.EnumManager;

namespace GameTemplate.Gameplay
{
    public class PathNode : MonoBehaviour
    {
        [SerializeField] BoxCollider2D _Box;
        [SerializeField] List<NodeNavigate> _Navigates;

        public Vector3 GetPosition(NodeNavigate Node)
        {
            switch (Node._NaviagteType)
            {
                default:
                case NavigateType.Random:
                    return GetRandomPosition();
                case NavigateType.Straight:
                    return
                        GetRandomStraight(this.transform.position,
                        Node._ConnectNode);
            }
        }

        public Vector3 GetRandomPosition()
        {
            Vector3 pos = this.transform.position;
            pos.x =
                this.transform.position.x +
                StaticFunction.RandomFloat(
                    -_Box.size.x / 2,
                    _Box.size.x / 2);
            pos.y =
                this.transform.position.y +
                StaticFunction.RandomFloat(
                    -_Box.size.y / 2,
                    _Box.size.y / 2);

            return pos;
        }

        public Vector3 GetRandomStraight(
            Vector3 currentpos,
            PathNode nextNode)
        {
            Vector3 offset = nextNode.transform.position - this.transform.position;
            Vector3 pos = currentpos + offset;
            return pos;
        }

        /// <summary>
        /// Get Next node on path
        /// </summary>
        /// <param name="FinishNode">Finish node</param>
        /// <returns>True is Finish node, Node return is next node </returns>
        public (bool, NodeNavigate) GetNextNode(PathNode FinishNode)
        {
            if (this == FinishNode)
            {
                return (true, null);
            }
            else
            {
                var nextNode = _Navigates.Find(x => x._FinishNode == FinishNode);
                return (false, nextNode);
            }
        }
    }

    [Serializable]
    public class NodeNavigate
    {
        public PathNode _FinishNode;
        public PathNode _ConnectNode;
        public NavigateType _NaviagteType;
    }
}
