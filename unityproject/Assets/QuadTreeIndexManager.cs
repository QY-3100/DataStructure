using System.Collections.Generic;
using UnityEngine;
using SmartCampusLogistics;

namespace SmartCampusLogistics
{
    public class QuadTree
    {
        private const int NODE_CAPACITY = 4;
        private Bounds bounds;
        private List<MapNode> containedNodes = new List<MapNode>();
        private QuadTree[] children = null;

        public QuadTree(Bounds bounds) { this.bounds = bounds; }

        public bool Insert(MapNode node)
        {
            if (!bounds.Contains(node.position)) return false;
            if (containedNodes.Count < NODE_CAPACITY && children == null) { containedNodes.Add(node); return true; }
            if (children == null) Subdivide();
            foreach (var child in children) { if (child.Insert(node)) return true; }
            return false;
        }

        private void Subdivide()
        {
            Vector3 subSize = bounds.size * 0.5f; Vector3 c = bounds.center;
            children = new QuadTree[4];
            children[0] = new QuadTree(new Bounds(new Vector3(c.x - subSize.x / 2, c.y, c.z + subSize.z / 2), subSize));
            children[1] = new QuadTree(new Bounds(new Vector3(c.x + subSize.x / 2, c.y, c.z + subSize.z / 2), subSize));
            children[2] = new QuadTree(new Bounds(new Vector3(c.x - subSize.x / 2, c.y, c.z - subSize.z / 2), subSize));
            children[3] = new QuadTree(new Bounds(new Vector3(c.x + subSize.x / 2, c.y, c.z - subSize.z / 2), subSize));
            foreach (var node in containedNodes) { foreach (var child in children) child.Insert(node); }
            containedNodes.Clear();
        }

        public List<MapNode> QueryRange(Bounds range, List<MapNode> result = null)
        {
            if (result == null) result = new List<MapNode>();
            if (!bounds.Intersects(range)) return result;
            foreach (var node in containedNodes) { if (range.Contains(node.position)) result.Add(node); }
            if (children != null) { foreach (var child in children) child.QueryRange(range, result); }
            return result;
        }
    }

    public class QuadTreeIndexManager : MonoBehaviour
    {
        public static QuadTreeIndexManager Instance { get; private set; }
        private QuadTree staticCampusQuadTree;

        private void Awake() { Instance = this; }

        private void Start()
        {
            staticCampusQuadTree = new QuadTree(new Bounds(Vector3.zero, new Vector3(1000f, 120f, 1000f)));
            if (MapGraphManager.Instance != null)
            {
                foreach (var n in MapGraphManager.Instance.allNodes) staticCampusQuadTree.Insert(n);
            }
            StartCoroutine(PeriodicalRoadClosureSimulation());
        }

        public MapNode GetClosestChargingNode(Vector3 carPos)
        {
            float radius = 40f;
            while (radius < 1500f)
            {
                Bounds searchBounds = new Bounds(carPos, new Vector3(radius, 100f, radius));
                List<MapNode> result = staticCampusQuadTree.QueryRange(searchBounds);

                List<MapNode> chargers = result.FindAll(n => n.type == NodeType.Parking);

                if (chargers.Count > 0)
                {
                    MapNode closest = chargers[0]; float d = Vector3.Distance(carPos, closest.position);
                    foreach (var c in chargers)
                    {
                        float dist = Vector3.Distance(carPos, c.position);
                        if (dist < d) { d = dist; closest = c; }
                    }
                    return closest;
                }
                radius += 80f;
            }
            return null;
        }

        /// <summary>
        /// 🎯 核心新增：在当前订单池中，为小车寻找距离其当前送达落脚点最近的取货订单
        /// </summary>
        public Order GetClosestOrder(MapNode currentCarNode, List<Order> availableOrders)
        {
            if (currentCarNode == null || availableOrders == null || availableOrders.Count == 0) return null;

            Order bestOrder = null;
            float minDistance = float.MaxValue;

            for (int i = 0; i < availableOrders.Count; i++)
            {
                Order order = availableOrders[i];
                if (order != null && order.pickupNode != null)
                {
                    float dist = Vector3.Distance(currentCarNode.position, order.pickupNode.position);

                    // 如果是加急订单，在距离权重上给予 200 米的插队降权福利，兼顾就近与紧急度
                    if (order.priority == OrderPriority.Urgent)
                    {
                        dist -= 200f;
                    }

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestOrder = order;
                    }
                }
            }
            return bestOrder;
        }

        /// <summary>
        /// 动态更新小车在空间系统中的索引位置
        /// </summary>
        public void UpdateAgentSpatialIndex(string carId, string statusInfo, MapNode currentNode)
        {
            // 接口平滑解耦
        }

        /// <summary>
        /// 周期性路网熔断仿真
        /// </summary>
        private System.Collections.IEnumerator PeriodicalRoadClosureSimulation()
        {
            while (true)
            {
                yield return new WaitForSeconds(18f);

                if (MapGraphManager.Instance != null && MapGraphManager.Instance.allNodes.Count > 0)
                {
                    var nodes = MapGraphManager.Instance.allNodes;
                    MapNode hazardNode = nodes[Random.Range(0, nodes.Count)];

                    List<MapEdge> closedEdges = new List<MapEdge>();
                    List<float> originalCosts = new List<float>();

                    foreach (var edge in hazardNode.edges)
                    {
                        if (!edge.isClosed)
                        {
                            closedEdges.Add(edge);
                            originalCosts.Add(edge.cost);

                            edge.isClosed = true;
                            edge.cost = float.MaxValue;
                        }
                    }

                    Debug.LogWarning("[Simulation] Traffic Closure at: " + hazardNode.nodeName);

                    DroneCarController[] cars = FindObjectsOfType<DroneCarController>();
                    foreach (var car in cars) car.RePlanPathOnTrafficJam();

                    if (closedEdges.Count > 0)
                    {
                        StartCoroutine(RestoreTrafficAfterDelay(closedEdges, originalCosts, hazardNode.nodeName));
                    }
                }
            }
        }

        /// <summary>
        /// 延时恢复交通网络
        /// </summary>
        private System.Collections.IEnumerator RestoreTrafficAfterDelay(List<MapEdge> edges, List<float> originalCosts, string nodeName)
        {
            yield return new WaitForSeconds(10f);

            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i] != null)
                {
                    edges[i].isClosed = false;
                    edges[i].cost = originalCosts[i];
                }
            }

            Debug.Log("<color=green>[Simulation] Traffic Restored at: " + nodeName + "</color>");
        }
    }
}