using UnityEngine;
using System.Collections.Generic;

public class Dispatcher : MonoBehaviour
{
    [Header("场景点位拖拽赋值")]
    public List<Transform> allPickPoints;
    public List<Transform> allDeliverPoints;
    public List<Transform> allChargePoints;
    public List<VehicleController> allVehicles;

    private PriorityQueue orderPool;
    public static Dispatcher Instance;
    private int orderAutoId = 1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        orderPool = new PriorityQueue();
    }

    void Start()
    {
        InvokeRepeating(nameof(GenerateNewOrder), 2f, 3f);
        InvokeRepeating(nameof(AllocateOrderToVehicle), 0.5f, 0.5f);
    }

    // 生成随机订单，严格匹配OrderData构造函数
    void GenerateNewOrder()
    {
        Transform randomPick = allPickPoints[Random.Range(0, allPickPoints.Count)];
        Transform randomDeliver = allDeliverPoints[Random.Range(0, allDeliverPoints.Count)];
        OrderPriority prio = Random.value > 0.6f ? OrderPriority.Urgent : OrderPriority.Normal;

        OrderData newOrder = new OrderData(orderAutoId, randomPick, randomDeliver, prio);
        orderAutoId++;

        // 加急权重更小，优先出队
        float weight = prio == OrderPriority.Urgent ? 0.1f : 1f;
        orderPool.Enqueue(newOrder, weight);

        Debug.Log($"生成订单 ID:{newOrder.orderId} 优先级:{prio} 取货:{randomPick.name} 送货:{randomDeliver.name}");
    }

    // 分配订单：找最近空闲、电量充足车辆
    void AllocateOrderToVehicle()
    {
        if (orderPool.Count == 0) return;

        OrderData targetOrder = orderPool.Dequeue();
        VehicleController bestVehicle = null;
        float minDist = float.MaxValue;

        foreach (VehicleController car in allVehicles)
        {
            // 过滤：忙碌 或 电量低于20，不分配订单
            if (car.isBusy || car.battery <= 20f)
                continue;

            float dist = Vector3.Distance(car.transform.position, targetOrder.pickupPoint.position);
            if (dist < minDist)
            {
                minDist = dist;
                bestVehicle = car;
            }
        }

        if (bestVehicle != null)
        {
            bestVehicle.AssignOrder(targetOrder);
            Debug.Log($"订单{targetOrder.orderId}分配给车辆{bestVehicle.vehicleId}");
        }
        else
        {
            // 无可用车辆，重新放回队列等待下一轮分配
            float weight = targetOrder.priority == OrderPriority.Urgent ? 0.1f : 1f;
            orderPool.Enqueue(targetOrder, weight);
            Debug.Log("暂无空闲/电量充足车辆，订单等待分配");
        }
    }

    // 内置最小堆优先队列，无外部依赖
    private class PriorityQueue
    {
        private List<(OrderData order, float weight)> heap = new List<(OrderData, float)>();
        public int Count => heap.Count;

        public void Enqueue(OrderData ord, float w)
        {
            heap.Add((ord, w));
            BubbleUp(heap.Count - 1);
        }

        public OrderData Dequeue()
        {
            var top = heap[0];
            int lastIdx = heap.Count - 1;
            heap[0] = heap[lastIdx];
            heap.RemoveAt(lastIdx);
            BubbleDown(0);
            return top.order;
        }

        void BubbleUp(int idx)
        {
            while (idx > 0)
            {
                int parent = (idx - 1) / 2;
                if (heap[idx].weight >= heap[parent].weight) break;
                Swap(idx, parent);
                idx = parent;
            }
        }

        void BubbleDown(int idx)
        {
            int last = heap.Count - 1;
            while (true)
            {
                int left = idx * 2 + 1;
                int right = idx * 2 + 2;
                int min = idx;
                if (left <= last && heap[left].weight < heap[min].weight) min = left;
                if (right <= last && heap[right].weight < heap[min].weight) min = right;
                if (min == idx) break;
                Swap(idx, min);
                idx = min;
            }
        }

        void Swap(int a, int b)
        {
            var temp = heap[a];
            heap[a] = heap[b];
            heap[b] = temp;
        }
    }
}