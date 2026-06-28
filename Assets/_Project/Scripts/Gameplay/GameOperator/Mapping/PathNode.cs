using System;
using System.Collections.Generic;
using UnityEngine;
using static GameTemplate.Core.EnumManager;

namespace GameTemplate.Gameplay
{
    public class PathNode : MonoBehaviour
    {
        [SerializeField] float _Diagonal;
        [SerializeField] float _AngleRad;
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
            
            (pos.x, pos.z) = 
                StaticFunction.RandomPointInRectangle
                (pos.x, pos.z, _Diagonal, _AngleRad);
        
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

        /// <summary>
        /// Get Next node on path
        /// </summary>
        /// <param name="FinishNode">Finish node</param>
        /// <returns>True is Finish node, Node return is next node </returns>
        public (bool, PathNode, Vector3) GetConnectNode(PathNode FinishNode)
        {
            var nextNavigate = _Navigates.Find(x => x._FinishNode == FinishNode);

            if (nextNavigate != null) return (false, null, Vector3.zero);

            return(
                nextNavigate._ConnectNode == FinishNode, 
                nextNavigate._ConnectNode, 
                GetPosition(nextNavigate));
        }

        public void DrawRectangleGizmo(Vector3 center, float diagonal, float angleRad)
        {
            float halfWidth = (diagonal / 2f) * Mathf.Cos(angleRad);
            float halfHeight = (diagonal / 2f) * Mathf.Sin(angleRad);

            // 4 góc của hình chữ nhật
            Vector3 topRight = center + new Vector3(halfWidth, center.y, halfHeight);
            Vector3 topLeft = center + new Vector3(-halfWidth, center.y, halfHeight);
            Vector3 bottomLeft = center + new Vector3(-halfWidth, center.y, -halfHeight);
            Vector3 bottomRight = center + new Vector3(halfWidth, center.y, -halfHeight);

            // Vẽ 4 cạnh
            Gizmos.color = Color.green;
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);

            // Vẽ 2 đường chéo
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(topLeft, bottomRight);
            Gizmos.DrawLine(topRight, bottomLeft);

            // Vẽ tâm
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(center, 0.05f);
        }

        private void OnDrawGizmos()
        {
            DrawRectangleGizmo(transform.position,  _Diagonal, _AngleRad);
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
