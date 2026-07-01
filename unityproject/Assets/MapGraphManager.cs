using System.Collections.Generic;
using UnityEngine;

namespace SmartCampusLogistics
{
    public enum NodeType { Building, Crossroads, PathNode, Parking }

    [System.Serializable]
    public class MapNode
    {
        public string nodeName;
        public Vector3 position;
        public NodeType type;

        [System.NonSerialized]
        public List<MapEdge> edges = new List<MapEdge>();
    }

    [System.Serializable]
    public class MapEdge
    {
        public MapNode toNode;
        public float originalDistance;
        public float cost;

        // 爷爷注意：这里帮您把之前重复定义报错的行删掉了，合并成干净的一个开关
        public bool isClosed = false;

        [Header("动态拥堵预测属性")]
        public float currentCongestionCoefficient = 0.0f;
        public float predictedCongestionIndex = 0.0f;

        [System.NonSerialized]
        private Queue<float> congestionWindow = new Queue<float>();
        private const int WINDOW_SIZE_K = 5;

        [Header("3D 表现层引用")]
        public Renderer roadRenderer;

        public void UpdateCongestionWithWindow(float newCoefficient)
        {
            currentCongestionCoefficient = Mathf.Clamp01(newCoefficient);

            if (congestionWindow == null) congestionWindow = new Queue<float>();
            congestionWindow.Enqueue(currentCongestionCoefficient);

            if (congestionWindow.Count > WINDOW_SIZE_K)
            {
                congestionWindow.Dequeue();
            }

            float sum = 0f;
            foreach (var coef in congestionWindow)
            {
                sum += coef;
            }
            predictedCongestionIndex = sum / congestionWindow.Count;

            this.cost = originalDistance * (1.0f + predictedCongestionIndex * 3.0f);

            UpdateRoadVisualEffect();
        }

        /// <summary>
        /// 🎨 道路变色镜：控制 3D 场景里马路的外观颜色
        /// </summary>
        public void UpdateRoadVisualEffect()
        {
            if (roadRenderer == null) return;

            Color targetColor;

            // 🚨 【核心修改】：如果是大红按钮封路，具有最高优先级，直接染成警报红！
            if (isClosed)
            {
                targetColor = new Color(1f, 0f, 0f); // 纯红警告
            }
            else if (predictedCongestionIndex < 0.3f)
            {
                targetColor = Color.green;
            }
            else if (predictedCongestionIndex < 0.7f)
            {
                float t = (predictedCongestionIndex - 0.3f) / 0.4f;
                targetColor = Color.Lerp(Color.green, new Color(1f, 0.6f, 0f), t);
            }
            else
            {
                float t = (predictedCongestionIndex - 0.7f) / 0.3f;
                targetColor = Color.Lerp(new Color(1f, 0.6f, 0f), Color.red, t);
            }

            // 把颜色刷到 Unity 的道路模型上
            if (roadRenderer.material.HasProperty("_BaseColor"))
            {
                roadRenderer.material.SetColor("_BaseColor", targetColor);
            }
            else
            {
                roadRenderer.material.color = targetColor;
            }
        }
    }

    public class MapGraphManager : MonoBehaviour
    {
        public static MapGraphManager Instance;
        public List<MapNode> allNodes = new List<MapNode>();
        private Dictionary<string, MapNode> nodeDictionary = new Dictionary<string, MapNode>();

        // 爷爷注意：这里为了让大红按钮能访问到所有的路，把 private 改成了公开的 public
        public List<MapEdge> allEdges = new List<MapEdge>();

        void Awake()
        {
            Instance = this;
            InitializeGraph();
        }

        public List<MapEdge> GetAllEdges()
        {
            return allEdges;
        }

        // ====== 🚨 【新装的功能】：大红按钮专属一键随机断路器 ======
        /// <summary>
        /// 随机把 3 到 5 条马路挂上禁行牌，并让它们在 3D 画面里变红
        /// </summary>
        public void InjectRandomFailures(int count)
        {
            if (allEdges == null || allEdges.Count == 0) return;

            // 1. 先把全校所有的马路恢复原状，解封清理现场
            ResetAllEdges();

            // 2. 把所有路扔进一个临时列表里进行洗牌打乱
            List<MapEdge> shuffleList = new List<MapEdge>(allEdges);
            for (int i = 0; i < shuffleList.Count; i++)
            {
                var temp = shuffleList[i];
                int randomIndex = Random.Range(i, shuffleList.Count);
                shuffleList[i] = shuffleList[randomIndex];
                shuffleList[randomIndex] = temp;
            }

            // 3. 抓出前几条路，翻开禁行牌，强制它们变成大红色
            int actualCount = Mathf.Min(count, shuffleList.Count);
            for (int i = 0; i < actualCount; i++)
            {
                shuffleList[i].isClosed = true;
                shuffleList[i].UpdateRoadVisualEffect(); // 刷成大红色
            }
        }

        /// <summary>
        /// 解封全校所有马路，恢复常态的绿色和正常通行的功能
        /// </summary>
        public void ResetAllEdges()
        {
            foreach (var edge in allEdges)
            {
                edge.isClosed = false;
                edge.UpdateRoadVisualEffect(); // 恢复原本根据拥堵决定的颜色
            }
        }
        // ===================================================================

        void InitializeGraph()
        {
            allNodes.Clear();
            nodeDictionary.Clear();
            allEdges.Clear();

            foreach (Transform child in transform)
            {
                if (child == null) continue;
                MapNode node = new MapNode
                {
                    nodeName = child.name,
                    position = child.position
                };

                string lowerName = child.name.ToLower();
                if (lowerName.Contains("cross")) node.type = NodeType.Crossroads;
                else if (lowerName.Contains("path")) node.type = NodeType.PathNode;
                else if (lowerName.Contains("parking")) node.type = NodeType.Parking;
                else node.type = NodeType.Building;

                allNodes.Add(node);
                nodeDictionary[child.name] = node;
            }

            LinkNodesTwoWay("Node_path6", "Node_cross001");
            LinkNodesTwoWay("Node_path6", "Node_0_station");
            LinkNodesTwoWay("Node_path6", "Node_2_dormB");
            LinkNodesTwoWay("Node_path6", "Node_parking_A_entry");
            LinkNodesTwoWay("Node_parking_A_entry", "Node_parking_A_area");

            LinkNodesTwoWay("Node_cross001", "Node_path1");
            LinkNodesTwoWay("Node_cross001", "Node_path5.1");

            LinkNodesTwoWay("Node_path1", "Node_path1.1");
            LinkNodesTwoWay("Node_path1", "Node_7_class2");
            LinkNodesTwoWay("Node_path1.1", "Node_cross002");
            LinkNodesTwoWay("Node_cross002", "Node_path2");

            LinkNodesTwoWay("Node_path2", "Node_cross003");
            LinkNodesTwoWay("Node_cross003", "Node_cross004");
            LinkNodesTwoWay("Node_cross003", "Node_path3");

            LinkNodesTwoWay("Node_path3", "Node_path3.1");
            LinkNodesTwoWay("Node_path3.1", "Node_path3.2");
            LinkNodesTwoWay("Node_path3.1", "Node_6_class1");
            LinkNodesTwoWay("Node_path3.2", "Node_cross006");

            LinkNodesTwoWay("Node_path5.1", "Node_cross006");
            LinkNodesTwoWay("Node_path5.1", "Node_1_dormA");
            LinkNodesTwoWay("Node_cross006", "Node_path5");
            LinkNodesTwoWay("Node_path5", "Node_cross005");
            LinkNodesTwoWay("Node_path5", "Node_3_canteen1");

            LinkNodesTwoWay("Node_cross005", "Node_path4.2");
            LinkNodesTwoWay("Node_cross005", "Node_4_canteen2");
            LinkNodesTwoWay("Node_4_canteen2", "Node_parking_B_entry");
            LinkNodesTwoWay("Node_parking_B_entry", "Node_parking_B_area");
            LinkNodesTwoWay("Node_path4.2", "Node_path4.1");
            LinkNodesTwoWay("Node_path4.1", "Node_path4");
            LinkNodesTwoWay("Node_path4.1", "Node_5_library");
            LinkNodesTwoWay("Node_path4", "Node_cross004");
        }

        void LinkNodesTwoWay(string nodeA, string nodeB)
        {
            LinkNodes(nodeA, nodeB);
            LinkNodes(nodeB, nodeA);
        }

        void LinkNodes(string fromName, string toName)
        {
            if (nodeDictionary.ContainsKey(fromName) && nodeDictionary.ContainsKey(toName))
            {
                MapNode from = nodeDictionary[fromName];
                MapNode to = nodeDictionary[toName];
                foreach (var e in from.edges) { if (e.toNode == to) return; }

                float dist = Vector3.Distance(from.position, to.position);

                MapEdge newEdge = new MapEdge
                {
                    toNode = to,
                    originalDistance = dist,
                    cost = dist,
                    isClosed = false
                };

                from.edges.Add(newEdge);
                allEdges.Add(newEdge);
            }
        }

        public MapNode GetNodeByName(string name)
        {
            return nodeDictionary.ContainsKey(name) ? nodeDictionary[name] : null;
        }

        public List<MapNode> FindPathAStar(MapNode start, MapNode end)
        {
            if (start == null || end == null) return null;

            List<MapNode> openSet = new List<MapNode> { start };
            HashSet<MapNode> closedSet = new HashSet<MapNode>();
            Dictionary<MapNode, MapNode> cameFrom = new Dictionary<MapNode, MapNode>();
            Dictionary<MapNode, float> gScore = new Dictionary<MapNode, float>();
            Dictionary<MapNode, float> fScore = new Dictionary<MapNode, float>();

            foreach (var node in allNodes)
            {
                gScore[node] = float.MaxValue;
                fScore[node] = float.MaxValue;
            }

            gScore[start] = 0;
            fScore[start] = Vector3.Distance(start.position, end.position);

            while (openSet.Count > 0)
            {
                MapNode current = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (fScore[openSet[i]] < fScore[current]) current = openSet[i];
                }

                if (current == end)
                {
                    List<MapNode> totalPath = new List<MapNode> { current };
                    while (cameFrom.ContainsKey(current))
                    {
                        current = cameFrom[current];
                        totalPath.Insert(0, current);
                    }
                    return totalPath;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var edge in current.edges)
                {
                    // 爷爷看这里：只要 edge.isClosed 变成 true 了，
                    // 小车计算 A* 路径时就会直接跳过这条路，实现智能动态绕行！
                    if (edge.isClosed || closedSet.Contains(edge.toNode)) continue;

                    float tentativeGScore = gScore[current] + edge.cost;

                    if (!openSet.Contains(edge.toNode)) openSet.Add(edge.toNode);
                    else if (tentativeGScore >= gScore[edge.toNode]) continue;

                    cameFrom[edge.toNode] = current;
                    gScore[edge.toNode] = tentativeGScore;
                    fScore[edge.toNode] = gScore[edge.toNode] + Vector3.Distance(edge.toNode.position, end.position);
                }
            }
            return null;
        }
    }
}