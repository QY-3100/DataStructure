using System;
using System.Collections.Generic;
using UnityEngine;
using SmartCampusLogistics;

namespace SmartCampusLogistics
{
    public class DroneCarController : MonoBehaviour
    {
        [Header("=== Base Attributes ===")]
        public string carId = "CAR-01";
        public string currentPositionNodeName = "Node_0_station";
        public float moveSpeed = 60f;
        [Range(0, 100)] public float battery = 100f;

        [Header("=== Visualization ===")]
        public LineRenderer pathLineRenderer;

        [Header("=== Simulation Properties ===")]
        public CarState currentState = CarState.Idle;
        public Order currentOrder = null;

        private Queue<MapNode> taskPathQueue = new Queue<MapNode>();
        private Stack<MapNode> historyStack = new Stack<MapNode>();

        private Vector3 virtualLogicalPosition;
        private Vector3 targetWorldPos;
        private bool isInitialized = false;
        private float stateTimer = 0f;
        private float debugTimer = 0f;

        private HashSet<string> localOrderBlacklist = new HashSet<string>();

        private string lastVisitedNodeName = "";
        private int pingPongCounter = 0;
        private float quadTreeSearchTimer = 0f;

        // ========================================================================================
        // 💡 提供给 UIToolkitManager 调用的属性，用于获取当前行驶路线的“下一站/终点站”目标
        // ========================================================================================
        public string TargetNodeName
        {
            get
            {
                if (currentState == CarState.GoingToPickup && currentOrder != null && currentOrder.pickupNode != null)
                {
                    return currentOrder.pickupNode.nodeName;
                }
                if (currentState == CarState.Delivering && currentOrder != null && currentOrder.deliveryNode != null)
                {
                    return currentOrder.deliveryNode.nodeName;
                }
                if (currentState == CarState.GoingToCharge)
                {
                    if (taskPathQueue != null && taskPathQueue.Count > 0)
                    {
                        MapNode[] nodes = taskPathQueue.ToArray();
                        return nodes[nodes.Length - 1].nodeName;
                    }
                    return "充电站";
                }
                return "";
            }
        }

        // ========================================================================================
        // 🚨 【核心注入点】：智能体抗灾原地一键重路由算法
        // ========================================================================================
        public void ForceReplanPathImmediate()
        {
            // 1. 如果小车没有在执行配送任务，直接跳过
            if (currentOrder == null) return;
            if (currentState != CarState.GoingToPickup && currentState != CarState.Delivering) return;

            Debug.LogWarning($"🤖 [智能体抗灾自愈] 车辆 {carId} 收到全网灾变预警！正在基于全新拓扑结构原地执行重路由...");

            // 2. 根据当前的配送状态，直接重新解算 A* 路径
            if (currentState == CarState.GoingToPickup)
            {
                List<MapNode> newPath = CallAStarRoute(currentPositionNodeName, currentOrder.pickupNode.nodeName);
                if (newPath != null && newPath.Count > 0) BuildTaskQueue(newPath);
            }
            else if (currentState == CarState.Delivering)
            {
                List<MapNode> newPath = CallAStarRoute(currentPositionNodeName, currentOrder.deliveryNode.nodeName);
                if (newPath != null && newPath.Count > 0) BuildTaskQueue(newPath);
            }
        }

        private void Start()
        {
            if (pathLineRenderer == null) pathLineRenderer = GetComponent<LineRenderer>();
            InvokeRepeating("ConsumeBattery", 1f, 1f);

            Rigidbody rb = GetComponent<Rigidbody>() ?? GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
            Collider col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
            if (col != null) { col.isTrigger = true; }
        }

        private void Update()
        {
            stateTimer += Time.deltaTime;
            debugTimer += Time.deltaTime;
            quadTreeSearchTimer += Time.deltaTime;

            if (!isInitialized && MapGraphManager.Instance != null)
            {
                ForceInitPositionToClosestNode();
            }

            if (!isInitialized) return;

            UpdateVisualPathLine();

            if (debugTimer > 3f)
            {
                Debug.Log($"<color=cyan>[{carId}] 状态: {currentState}, 剩余路径节点数: {taskPathQueue.Count}, 电量: {battery:F1}%</color>");
                debugTimer = 0f;
            }

            if (battery <= 0.01f)
            {
                if (currentState != CarState.Idle || taskPathQueue.Count > 0 || currentOrder != null)
                {
                    Debug.LogError($"🚨 [{carId}] 电量完全耗尽！退回当前订单，清空路由。");
                    if (currentOrder != null)
                    {
                        ReturnOrderToSystemPool(currentOrder);
                        currentOrder = null;
                    }
                    taskPathQueue.Clear();
                    currentState = CarState.Idle;
                    stateTimer = -9999f;
                }
                return;
            }

            if (battery < 25f && currentState != CarState.GoingToCharge && currentState != CarState.Charging)
            {
                Debug.LogWarning($"⚠️ [{carId}] 任务中动态低电量熔断触发！当前电量 {battery:F1}% < 25%。强行前往充电。");
                if (currentOrder != null)
                {
                    localOrderBlacklist.Add(currentOrder.orderId);
                    ReturnOrderToSystemPool(currentOrder);
                    currentOrder = null;
                }
                taskPathQueue.Clear();
                GoToNearestChargingStation();
                return;
            }

            if (currentState == CarState.Idle)
            {
                if (stateTimer < 0f) return;

                if (battery < 30f)
                {
                    Debug.LogWarning($"⚡ [{carId}] 空闲蓄能拦截：当前电量 {battery:F1}% < 30%，驶向无线充电桩。");
                    GoToNearestChargingStation();
                    return;
                }

                if (OrderAndPathSystem.Instance != null)
                {
                    MapNode currentNode = MapGraphManager.Instance?.GetNodeByName(currentPositionNodeName);
                    OrderAndPathSystem.Instance.RequestDispatchAssignment(this, currentNode);
                }
            }
            else if (currentState != CarState.Charging)
            {
                ExecuteStrictNodeMovement();
            }
        }

        private void ForceInitPositionToClosestNode()
        {
            if (MapGraphManager.Instance == null) return;

            MapNode bestNode = null;
            if (MapGraphManager.Instance.allNodes != null)
            {
                bestNode = MapGraphManager.Instance.allNodes.Find(n => n.nodeName == currentPositionNodeName);
            }

            if (bestNode == null && MapGraphManager.Instance.allNodes != null && MapGraphManager.Instance.allNodes.Count > 0)
            {
                float minDist = float.MaxValue;
                foreach (var node in MapGraphManager.Instance.allNodes)
                {
                    float dist = Vector3.Distance(transform.position, node.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        bestNode = node;
                    }
                }
            }

            if (bestNode != null)
            {
                currentPositionNodeName = bestNode.nodeName;
                virtualLogicalPosition = bestNode.position;
                virtualLogicalPosition.y = transform.position.y;
                transform.position = virtualLogicalPosition;
                isInitialized = true;
                Debug.Log($"<color=green>[{carId}] 成功对齐路网节点起点: {currentPositionNodeName}</color>");
            }
        }

        public void AllocateOrder(Order order)
        {
            if (order == null) return;

            if (localOrderBlacklist.Contains(order.orderId) || battery < 35f)
            {
                ReturnOrderToSystemPool(order);
                return;
            }

            if (currentPositionNodeName == order.pickupNode.nodeName)
            {
                currentOrder = order;
                currentState = CarState.GoingToPickup;
                OnReachedSegmentDestination();
                return;
            }

            currentOrder = order;
            currentState = CarState.GoingToPickup;
            stateTimer = 0f;

            List<MapNode> path = CallAStarRoute(currentPositionNodeName, order.pickupNode.nodeName);

            if (path != null && path.Count > 0)
            {
                BuildTaskQueue(path);
            }
            else
            {
                localOrderBlacklist.Add(order.orderId);
                ReturnOrderToSystemPool(order);
                currentOrder = null;
                taskPathQueue.Clear();
                currentState = CarState.Idle;
                stateTimer = -5f;
            }
        }

        private void ExecuteStrictNodeMovement()
        {
            if (taskPathQueue.Count == 0)
            {
                OnReachedSegmentDestination();
                return;
            }

            MapNode nextNode = taskPathQueue.Peek();
            targetWorldPos = nextNode.position;
            targetWorldPos.y = virtualLogicalPosition.y;

            virtualLogicalPosition = Vector3.MoveTowards(virtualLogicalPosition, targetWorldPos, moveSpeed * Time.deltaTime);
            transform.position = virtualLogicalPosition;

            Vector3 moveDir = targetWorldPos - virtualLogicalPosition; moveDir.y = 0;
            if (moveDir.sqrMagnitude > 0.02f)
            {
                Quaternion lookRot = Quaternion.LookRotation(moveDir);
                Quaternion correctedRot = lookRot * Quaternion.Euler(0, 180, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, correctedRot, Time.deltaTime * 12f);
            }

            if (Vector3.Distance(virtualLogicalPosition, targetWorldPos) < 1.0f)
            {
                MapNode reachedNode = taskPathQueue.Dequeue();
                string previousNodeName = currentPositionNodeName;
                currentPositionNodeName = reachedNode.nodeName;
                historyStack.Push(reachedNode);
                stateTimer = 0f;

                if (currentPositionNodeName == lastVisitedNodeName && previousNodeName != currentPositionNodeName)
                {
                    if (pingPongCounter >= 2)
                    {
                        Debug.LogWarning($"⚠️ [{carId}] 拓扑死锁熔断：在 {previousNodeName} 与 {currentPositionNodeName} 之间摩擦！退单恢复。");
                        if (currentOrder != null)
                        {
                            localOrderBlacklist.Add(currentOrder.orderId);
                            ReturnOrderToSystemPool(currentOrder);
                            currentOrder = null;
                        }
                        taskPathQueue.Clear();
                        currentState = CarState.Idle;
                        stateTimer = -6f;
                        pingPongCounter = 0;
                        lastVisitedNodeName = "";
                        return;
                    }
                    pingPongCounter++;
                }
                else
                {
                    if (previousNodeName != currentPositionNodeName)
                    {
                        lastVisitedNodeName = previousNodeName;
                    }
                }
            }
        }

        private void OnReachedSegmentDestination()
        {
            lastVisitedNodeName = "";
            pingPongCounter = 0;

            if (currentState == CarState.GoingToPickup && currentOrder != null)
            {
                currentState = CarState.Delivering;
                stateTimer = 0f;

                List<MapNode> nextPath = CallAStarRoute(currentPositionNodeName, currentOrder.deliveryNode.nodeName);
                if (nextPath != null && nextPath.Count > 0)
                {
                    BuildTaskQueue(nextPath);
                }
                else
                {
                    ReturnOrderToSystemPool(currentOrder);
                    currentOrder = null;
                    currentState = CarState.Idle;
                    taskPathQueue.Clear();
                }
            }
            else if (currentState == CarState.Delivering)
            {
                localOrderBlacklist.Clear();
                currentOrder = null;

                if (battery <= 35f)
                {
                    Debug.LogWarning($"⚠️ [{carId}] 稳态拦截：送达后电量低 ({battery:F1}%)，拒绝后续接单，强制前往充电站。");
                    GoToNearestChargingStation();
                }
                else
                {
                    currentState = CarState.Idle;
                }
            }
            else if (currentState == CarState.GoingToCharge)
            {
                StartCoroutine(ChargingProcessRoutine());
            }
        }

        private void ReturnOrderToSystemPool(Order order)
        {
            if (order == null) return;
            if (OrderAndPathSystem.Instance != null)
            {
                OrderAndPathSystem.Instance.ReturnOrderToSystemPool(order);
            }
        }

        public void RePlanPathOnTrafficJam()
        {
            if (currentOrder == null) return;
            if (currentState == CarState.GoingToPickup)
            {
                var path = CallAStarRoute(currentPositionNodeName, currentOrder.pickupNode.nodeName);
                if (path != null) BuildTaskQueue(path);
            }
            else if (currentState == CarState.Delivering)
            {
                List<MapNode> p = CallAStarRoute(currentPositionNodeName, currentOrder.deliveryNode.nodeName);
                if (p != null) BuildTaskQueue(p);
            }
        }

        private void GoToNearestChargingStation()
        {
            if (CheckAndRouteToChargingStation())
            {
                return;
            }

            currentState = CarState.GoingToCharge;
            List<MapNode> bestPath = null;

            if (QuadTreeIndexManager.Instance != null)
            {
                MapNode chargeNode = QuadTreeIndexManager.Instance.GetClosestChargingNode(virtualLogicalPosition);
                if (chargeNode != null)
                {
                    bestPath = CallAStarRoute(currentPositionNodeName, chargeNode.nodeName);
                }
            }

            if (bestPath == null || bestPath.Count == 0)
            {
                string[] fallbackStations = { "Node_parking_A_area", "Node_parking_B_area" };
                float minDistance = float.MaxValue;

                foreach (var stationName in fallbackStations)
                {
                    List<MapNode> testPath = CallAStarRoute(currentPositionNodeName, stationName);
                    if (testPath != null && testPath.Count > 0)
                    {
                        float dist = Vector3.Distance(transform.position, testPath[testPath.Count - 1].position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestPath = testPath;
                        }
                    }
                }
            }

            if (bestPath != null && bestPath.Count > 0)
            {
                BuildTaskQueue(bestPath);
                Debug.Log($"<color=orange>⚡ [{carId}] 充电路由规划成功，目标站：{bestPath[bestPath.Count - 1].nodeName}</color>");
            }
            else
            {
                Debug.LogError($"🚨 [{carId}] 路网完全拓扑断开！无法通过路径前往充电桩。执行紧急物理常驻接轨。");
                currentPositionNodeName = "Node_parking_A_area";
                if (MapGraphManager.Instance != null)
                {
                    MapNode node = MapGraphManager.Instance.GetNodeByName(currentPositionNodeName);
                    if (node != null) transform.position = node.position;
                }
                battery = 100f;
                currentState = CarState.Idle;
            }
        }

        private List<MapNode> CallAStarRoute(string startName, string endName)
        {
            if (MapGraphManager.Instance == null) return null;
            MapNode startNode = MapGraphManager.Instance.GetNodeByName(startName);
            MapNode endNode = MapGraphManager.Instance.GetNodeByName(endName);
            if (startNode == null || endNode == null) return null;
            return MapGraphManager.Instance.FindPathAStar(startNode, endNode);
        }

        private void BuildTaskQueue(List<MapNode> nodes)
        {
            taskPathQueue.Clear();
            if (nodes == null) return;

            int startIndex = 0;
            if (nodes.Count > 0 && nodes[0].nodeName == currentPositionNodeName)
            {
                startIndex = 1;
            }

            for (int i = startIndex; i < nodes.Count; i++)
            {
                taskPathQueue.Enqueue(nodes[i]);
            }
        }

        private void ConsumeBattery()
        {
            if (battery <= 0) return;
            bool isMoving = (currentState == CarState.GoingToPickup || currentState == CarState.Delivering || currentState == CarState.GoingToCharge);
            battery -= isMoving ? 0.1f : 0.01f;
            if (battery < 0) battery = 0;
        }

        private void UpdateVisualPathLine()
        {
            if (pathLineRenderer == null) return;
            if (taskPathQueue == null || taskPathQueue.Count == 0 || currentState == CarState.Idle)
            {
                pathLineRenderer.positionCount = 0;
                return;
            }

            MapNode[] array = taskPathQueue.ToArray();
            pathLineRenderer.positionCount = array.Length + 1;
            pathLineRenderer.SetPosition(0, virtualLogicalPosition);

            for (int i = 0; i < array.Length; i++)
            {
                pathLineRenderer.SetPosition(i + 1, array[i].position + Vector3.up * 0.5f);
            }

            if (currentState == CarState.GoingToCharge)
            {
                pathLineRenderer.startColor = Color.yellow;
                pathLineRenderer.endColor = new Color(1f, 0.5f, 0f);
            }
            else if (currentOrder != null && currentOrder.priority == OrderPriority.Urgent)
            {
                pathLineRenderer.startColor = Color.red;
                pathLineRenderer.endColor = new Color(1f, 0.4f, 0.4f);
            }
            else
            {
                pathLineRenderer.startColor = Color.green;
                pathLineRenderer.endColor = Color.cyan;
            }
        }

        private System.Collections.IEnumerator ChargingProcessRoutine()
        {
            currentState = CarState.Charging;
            localOrderBlacklist.Clear();

            Debug.Log($"<color=yellow>⚡ [{carId}] 已成功对接无线充电网格，进入强电稳态输入状态...</color>");

            while (battery < 100f)
            {
                battery += 15f * Time.deltaTime;
                if (battery > 100f) battery = 100f;
                yield return null;
            }

            Debug.Log($"<color=green>🟢 [{carId}] 稳态蓄能完毕（100%）！解除锁定，重新投入车队排班。</color>");

            currentState = CarState.Idle;
            stateTimer = 0f;
        }

        public string GetCarStatusTextTranslation()
        {
            string baseStatus = "";
            switch (currentState)
            {
                case CarState.Idle:
                    baseStatus = stateTimer < 0f ? "熔断冷却中" : "空闲待命";
                    break;
                case CarState.GoingToPickup:
                    baseStatus = "前往取货中";
                    break;
                case CarState.Delivering:
                    baseStatus = "全速配送中";
                    break;
                case CarState.GoingToCharge:
                    baseStatus = "驶向充电桩";
                    break;
                case CarState.Charging:
                    baseStatus = "正在充电";
                    break;
                default:
                    baseStatus = "未知状态";
                    break;
            }

            string target = TargetNodeName;
            if (!string.IsNullOrEmpty(target))
            {
                return $"{baseStatus} ▢ ➜ {target}";
            }

            return baseStatus;
        }

        private bool CheckAndRouteToChargingStation()
        {
            if (battery > 25f) return false;

            if (QuadTreeIndexManager.Instance == null) return false;

            MapNode closestStation = QuadTreeIndexManager.Instance.GetClosestChargingNode(virtualLogicalPosition);
            if (closestStation == null) return false;

            List<MapNode> pathToStation = CallAStarRoute(currentPositionNodeName, closestStation.nodeName);
            if (pathToStation == null || pathToStation.Count == 0) return false;

            float estimatedCost = 0f;
            for (int i = 0; i < pathToStation.Count - 1; i++)
            {
                float distance = Vector3.Distance(pathToStation[i].position, pathToStation[i + 1].position);
                estimatedCost += distance * 0.05f;
            }

            float safetyRequiredBattery = estimatedCost * 1.1f;

            if (battery < safetyRequiredBattery)
            {
                Debug.LogError($"<color=red>[紧急救援] 小车 {carId} 剩余电量 {battery}%，去最近充电站理论上最低需耗电 {safetyRequiredBattery}%！强行前往必死。已触发原地停靠等待抛锚/熔断重入逻辑。</color>");

                if (currentOrder != null)
                {
                    ReturnOrderToSystemPool(currentOrder);
                    currentOrder = null;
                }

                taskPathQueue.Clear();
                currentState = CarState.Idle;
                stateTimer = -999f;
                return true;
            }

            BuildTaskQueue(pathToStation);
            currentState = CarState.GoingToCharge;
            return true;
        }
    }
}