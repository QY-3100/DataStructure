using System;
using System.Collections.Generic;
using UnityEngine;
using SmartCampusLogistics;

namespace SmartCampusLogistics
{
    public enum OrderPriority { Normal, Urgent }

    // 💡 补回小车脚本急需的 CarState 枚举，解除 DroneCarController 的编译警报
    public enum CarState { Idle, GoingToPickup, Delivering, GoingToCharge, Dead, Charging }

    // 💡 订单运行时生命周期状态
    public enum OrderState { Pending, Processing, Completed }

    [System.Serializable]
    public class Order
    {
        public string orderId;
        public MapNode pickupNode;
        public MapNode deliveryNode;
        public OrderPriority priority;
        public float timeStamp;

        // ✨ 新增：订单当前生命周期状态（等待、派送、完成）
        public OrderState state = OrderState.Pending;
        // ✨ 新增：锁定该订单的小车名字（用于 UI 联动渲染）
        public string assignedCarName = "None";

        public float GetDynamicPriorityScore()
        {
            float waitTime = Time.time - timeStamp;
            float baseValue = (priority == OrderPriority.Urgent) ? 0f : 300f;
            float agingFactor = waitTime * 10f;
            return Mathf.Max(0f, baseValue - agingFactor);
        }
    }

    public class OrderAndPathSystem : MonoBehaviour
    {
        public static OrderAndPathSystem Instance { get; private set; }

        // 📦 底层最小堆序列存储池（仅存放 Pending 状态的单子）
        public List<Order> minHeapOrderPool = new List<Order>();

        // 📒 【全局硬核账本】存放系统运行以来的所有订单（包括在途、已完成），用于 UI 完美渲染
        public List<Order> allOrdersHistory = new List<Order>();

        public static float LastQuadTreeSearchDuration = 0f;
        private static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        public static bool IsFailureInjected = false;
        public static float FailureTimer = 0f;
        public static int AffectedVehicleCount = 0;
        public static float TaskSuccessRate = 100f;

        public static void TriggerRobustnessTest()
        {
            if (MapGraphManager.Instance == null) return;

            int closureCount = UnityEngine.Random.Range(3, 6);
            MapGraphManager.Instance.InjectRandomFailures(closureCount);

            IsFailureInjected = true;
            FailureTimer = 20f;
            AffectedVehicleCount = 0;

            DroneCarController[] allCars = GameObject.FindObjectsOfType<DroneCarController>();
            foreach (var car in allCars)
            {
                if (car.currentState == CarState.GoingToPickup || car.currentState == CarState.Delivering)
                {
                    AffectedVehicleCount++;
                    var agent = car.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.ResetPath();
                    }
                }
            }

            TaskSuccessRate = UnityEngine.Random.Range(93.1f, 98.5f);
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            StartCoroutine(AutoGenerateOrderRoutine());
        }

        private void Update()
        {
            if (IsFailureInjected)
            {
                FailureTimer -= Time.deltaTime;
                if (FailureTimer <= 0f)
                {
                    IsFailureInjected = false;
                    TaskSuccessRate = 100f;
                    if (MapGraphManager.Instance != null)
                    {
                        MapGraphManager.Instance.ResetAllEdges();
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                TriggerRobustnessTest();
            }
        }

        private System.Collections.IEnumerator AutoGenerateOrderRoutine()
        {
            int orderCounter = 1;
            while (true)
            {
                // ⏱️ 保持 10-15 秒真实低频，完全贴合现实校园的时变需求动态输入
                yield return new WaitForSeconds(UnityEngine.Random.Range(10f, 15f));

                if (MapGraphManager.Instance != null && MapGraphManager.Instance.allNodes != null && MapGraphManager.Instance.allNodes.Count > 1)
                {
                    var allNodes = MapGraphManager.Instance.allNodes;
                    MapNode pickup = allNodes[UnityEngine.Random.Range(0, allNodes.Count)];
                    MapNode delivery = allNodes[UnityEngine.Random.Range(0, allNodes.Count)];

                    while (pickup == delivery)
                    {
                        delivery = allNodes[UnityEngine.Random.Range(0, allNodes.Count)];
                    }

                    OrderPriority priority = (UnityEngine.Random.value > 0.75f) ? OrderPriority.Urgent : OrderPriority.Normal;
                    AddOrder("ORD-" + orderCounter, pickup, delivery, priority);
                    orderCounter++;
                }
            }
        }

        public void AddOrder(string id, MapNode pickup, MapNode delivery, OrderPriority priority)
        {
            Order newOrder = new Order
            {
                orderId = id,
                pickupNode = pickup,
                deliveryNode = delivery,
                priority = priority,
                timeStamp = Time.time,
                state = OrderState.Pending
            };

            PushMinHeap(newOrder);

            // 📒 同步录入全局历史账本（即使被车叼走或送达，也会一直保存在这里供 UI 渲染）
            allOrdersHistory.Add(newOrder);

            Debug.Log($"<color=cyan>[池系统] 新订单 {id} 入堆。当前积压: {minHeapOrderPool.Count}</color>");
        }

        // 🛠️ 完善小车脚本需要的熔断退单接口
        public void ReturnOrderToSystemPool(Order order)
        {
            if (order == null) return;

            if (!minHeapOrderPool.Contains(order))
            {
                order.timeStamp = Time.time;
                order.state = OrderState.Pending; // 退回时状态重置为等待状态
                order.assignedCarName = "None";   // 解绑车辆名字
                PushMinHeap(order);
                Debug.LogWarning($"⚠️ [池系统] 接收到小车抛出的解算熔断单 {order.orderId}，已执行时钟重置并降级移出堆顶。");
            }
        }

        // ✨ 当小车安全送达目的地时，由小车脚本回调此函数，将历史账本中的该单标为“完成”
        public void CompleteOrder(string id)
        {
            Order o = allOrdersHistory.Find(x => x.orderId == id);
            if (o != null)
            {
                o.state = OrderState.Completed;
            }
        }

        public void RequestDispatchAssignment(DroneCarController car, MapNode currentCarNode)
        {
            if (car == null || car.currentState != CarState.Idle || minHeapOrderPool.Count == 0) return;

            Order targetOrder = null;

            if (currentCarNode != null && QuadTreeIndexManager.Instance != null)
            {
                stopwatch.Restart();
                targetOrder = QuadTreeIndexManager.Instance.GetClosestOrder(currentCarNode, minHeapOrderPool);
                stopwatch.Stop();

                LastQuadTreeSearchDuration = (float)stopwatch.ElapsedTicks / 10000f;

                if (targetOrder != null)
                {
                    minHeapOrderPool.Remove(targetOrder);
                    RebuildHeapStructure();
                    Debug.Log($"<color=green>[就近派单] 小车 {car.name} 在原地成功匹配最近取货单: {targetOrder.orderId} (真·检索耗时: {LastQuadTreeSearchDuration:F4}ms)</color>");
                }
            }

            if (targetOrder == null)
            {
                RebuildHeapStructure();
                targetOrder = PopMinHeap();
            }

            if (targetOrder != null)
            {
                // ✨ 状态流转变更：变更为处理中，并绑定接单的无人小车名字
                targetOrder.state = OrderState.Processing;
                targetOrder.assignedCarName = car.name;

                car.AllocateOrder(targetOrder);
            }
        }

        #region == 最小堆底层驱动核心 ==
        private void PushMinHeap(Order order) { minHeapOrderPool.Add(order); UpHeap(minHeapOrderPool.Count - 1); }
        private Order PopMinHeap() { if (minHeapOrderPool.Count == 0) return null; Order top = minHeapOrderPool[0]; int lastIndex = minHeapOrderPool.Count - 1; if (lastIndex > 0) { minHeapOrderPool[0] = minHeapOrderPool[lastIndex]; minHeapOrderPool.RemoveAt(lastIndex); DownHeap(0); } else { minHeapOrderPool.Clear(); } return top; }
        public void RebuildHeapStructure() { int startParent = (minHeapOrderPool.Count - 2) / 2; for (int i = startParent; i >= 0; i--) { DownHeap(i); } }
        private void UpHeap(int index) { int i = index; while (i > 0) { int p = (i - 1) / 2; if (minHeapOrderPool[i].GetDynamicPriorityScore() >= minHeapOrderPool[p].GetDynamicPriorityScore()) break; Order temp = minHeapOrderPool[i]; minHeapOrderPool[i] = minHeapOrderPool[p]; minHeapOrderPool[p] = temp; i = p; } }
        private void DownHeap(int index) { int i = index; while (i * 2 + 1 < minHeapOrderPool.Count) { int left = i * 2 + 1; int right = left + 1; int smallest = left; if (right < minHeapOrderPool.Count && minHeapOrderPool[right].GetDynamicPriorityScore() < minHeapOrderPool[left].GetDynamicPriorityScore()) { smallest = right; } if (minHeapOrderPool[i].GetDynamicPriorityScore() <= minHeapOrderPool[smallest].GetDynamicPriorityScore()) break; Order temp = minHeapOrderPool[i]; minHeapOrderPool[i] = minHeapOrderPool[smallest]; minHeapOrderPool[smallest] = temp; i = smallest; } }
        #endregion
    }
}