using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SmartCampusLogistics
{
    public class RoadNetworkSensorSimulator : MonoBehaviour
    {
        [Header("传感器基本配置")]
        [Tooltip("扫描并更新全网路况的周期时间（秒）")]
        public float scanInterval = 2.0f;          // 扫描路网路况的间隔时间
        [Tooltip("小车基础耗电系数")]
        public float baseDischargeRate = 0.5f;     // 小车基础耗电系数

        [Header("答辩高级特性模拟（可选）")]
        [Tooltip("是否开启随机路段临时封闭模拟（用于容错与自动重规划演示）")]
        public bool enableRandomClosureMock = false;
        [Range(0f, 1f)]
        [Tooltip("每轮扫描触发路段封闭变更的概率")]
        public float closureProbability = 0.05f;

        private float timer = 0f;

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= scanInterval)
            {
                timer = 0f;
                MonitorRoadNetworkStatus();
            }
        }

        /// <summary>
        /// 核心：扫描并感知所有路网节点与边的负载、计算滑动窗口预测值并监控熔断状态
        /// </summary>
        public void MonitorRoadNetworkStatus()
        {
            if (MapGraphManager.Instance == null) return;

            // 1. 遍历路网中所有的 MapNode 节点
            foreach (MapNode node in MapGraphManager.Instance.allNodes)
            {
                if (node == null || node.edges == null) continue;

                // 2. 遍历该节点延伸出去的每一条边（MapEdge）
                foreach (MapEdge edge in node.edges)
                {
                    if (edge == null) continue;

                    // ==================== 【时空自适应：滑动窗口拥堵数据注入】 ====================
                    // 模拟实时传感器采集的环境原始噪声 (0.0 到 1.0)
                    // 在实际应用中此处可更换为基于时间的潮汐函数
                    float mockCongestionNoise = Random.Range(0.0f, 1.0f);

                    // 核心打通：调用带 K=5 滑动窗口平滑滤波的更新接口，同时它会联动 A* 代价膨胀和 3D 发光变色
                    edge.UpdateCongestionWithWindow(mockCongestionNoise);
                    // =========================================================================

                    // 3. 模拟恶劣天气/事故导致的突发道路临时封闭
                    if (enableRandomClosureMock && Random.value < closureProbability)
                    {
                        edge.isClosed = !edge.isClosed;
                    }

                    // 如果该路段被标记为关闭/熔断，输出警告，供 D* Lite / A* 触发即时重规划
                    if (edge.isClosed)
                    {
                        Debug.LogWarning($"[路网时空拓扑感知] 监测到熔断路径: {node.nodeName} -> {edge.toNode.nodeName}，正在通知调度中心执行自适应重规划。");
                    }
                }
            }
        }

        /// <summary>
        /// 核心：为低电量小车评估并规划前往最近充电桩的安全路径
        /// </summary>
        /// <param name="carTransform">小车物体的 Transform</param>
        /// <param name="currentNode">小车当前所在的 MapNode 节点</param>
        /// <param name="currentBattery">小车当前剩余电量</param>
        /// <param name="outPathToStation">输出规划成功的路径节点列表</param>
        /// <returns>是否成功规划并允许前往充电桩</returns>
        public bool EvaluateChargingRoute(Transform carTransform, MapNode currentNode, float currentBattery, out List<MapNode> outPathToStation)
        {
            outPathToStation = null;

            if (currentNode == null || MapGraphManager.Instance == null) return false;

            // 1. 寻找物理空间上距离小车最近的充电区域节点（根据全网节点类型检索）
            MapNode closestStation = null;
            float minDistance = float.MaxValue;

            foreach (var node in MapGraphManager.Instance.allNodes)
            {
                if (node == null) continue;

                // 筛选出属于充电泊车区的节点
                if (node.type == NodeType.Parking && node.nodeName.ToLower().Contains("area"))
                {
                    float dist = Vector3.Distance(carTransform.position, node.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestStation = node;
                    }
                }
            }

            if (closestStation == null) return false;

            // 2. 调用正牌单例 MapGraphManager.Instance.FindPathAStar
            // 注意：此时 A* 算法内部解算时，读取的 edge.cost 已经是经过“滑动窗口未来拥堵指数预测”修正后的代价！
            // 能够极大确保电量紧缺的小车在返航时避开即将拥堵的路段，实现“绿色低能耗安全反航”。
            List<MapNode> pathToStation = MapGraphManager.Instance.FindPathAStar(currentNode, closestStation);
            if (pathToStation == null || pathToStation.Count == 0) return false;

            // 3. 严谨评估整条路径的预计耗电量
            float estimatedEnergyCost = 0f;
            for (int i = 0; i < pathToStation.Count - 1; i++)
            {
                float distance = Vector3.Distance(pathToStation[i].position, pathToStation[i + 1].position);
                estimatedEnergyCost += distance * baseDischargeRate;
            }

            // 加上 30% 的电池安全冗余储备
            float safetyRequiredBattery = estimatedEnergyCost * 1.3f;

            // 4. 判断当前电量是否足够支撑小车开到充电桩
            if (currentBattery < safetyRequiredBattery)
            {
                Debug.LogError($"<color=red>[时空调度异常] 警告：小车剩余电量 {currentBattery}% 严重不足！预计返航最低安全电量需 {safetyRequiredBattery}%。强行呼叫就地挂起救援。</color>");
                return false;
            }

            // 允许前往，输出路径
            outPathToStation = pathToStation;
            return true;
        }
    }
}