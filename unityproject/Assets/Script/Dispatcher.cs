using System.Collections.Generic;
using UnityEngine;

public class Dispatcher : MonoBehaviour
{
    private SimplePriorityQueue<Order> orderQueue;
    private VehicleManager vehicleManager;
    private OrderGenerator orderGenerator;

    public delegate void OrderEvent(Order order);
    public event OrderEvent OnOrderEnqueued;
    public event OrderEvent OnOrderAssigned;

    void Awake()
    {
        orderQueue = new SimplePriorityQueue<Order>();
    }

    void Start()
    {
        vehicleManager = FindObjectOfType<VehicleManager>();
        orderGenerator = FindObjectOfType<OrderGenerator>();
        
        if (orderGenerator != null)
        {
            orderGenerator.OnOrderGenerated += OnNewOrder;
        }
    }

    void OnNewOrder(Order order)
    {
        EnqueueOrder(order);
    }

    public void EnqueueOrder(Order order)
    {
        orderQueue.Enqueue(order, order.GetWeight());
        Debug.Log($"Order enqueued: {order.OrderId}, Weight: {order.GetWeight()}");
        Debug.Log($"OnOrderEnqueued event has {OnOrderEnqueued?.GetInvocationList().Length ?? 0} subscribers");
        OnOrderEnqueued?.Invoke(order);
    }

    public void DispatchOrders()
    {
        while (orderQueue.Count > 0)
        {
            Order order = orderQueue.Dequeue();
            AssignOrderToVehicle(order);
        }
    }

    private void AssignOrderToVehicle(Order order)
    {
        if (vehicleManager == null)
        {
            Debug.LogError("VehicleManager not found!");
            return;
        }

        DeliveryVehicle bestVehicle = FindBestVehicle(order);
        
        if (bestVehicle != null)
        {
            if (order.Priority == PriorityLevel.Urgent)
            {
                bestVehicle.InsertUrgentOrderBeforeFirstNormal(order);
            }
            else
            {
                bestVehicle.AddTaskToEnd(new DeliveryTask(order));
            }
            Debug.Log($"Order {order.OrderId} assigned to Vehicle {bestVehicle.VehicleId}");
            OnOrderAssigned?.Invoke(order);
        }
        else
        {
            EnqueueOrder(order);
            Debug.Log($"No available vehicle for Order {order.OrderId}, re-enqueued");
        }
    }

    private DeliveryVehicle FindBestVehicle(Order order)
    {
        DeliveryVehicle bestVehicle = null;
        float bestScore = float.MaxValue;

        foreach (var vehicle in vehicleManager.GetVehicles())
        {
            if (vehicle.NeedsCharging())
                continue;

            float distance = Vector2.Distance(
                new Vector2(vehicle.transform.position.x, vehicle.transform.position.z),
                order.PickPoint
            );

            float loadFactor = vehicle.TaskCount;
            
            float score = distance + loadFactor * 10;

            if (score < bestScore)
            {
                bestScore = score;
                bestVehicle = vehicle;
            }
        }

        return bestVehicle;
    }

    public int GetPendingOrderCount()
    {
        int count = orderQueue == null ? 0 : orderQueue.Count;
        Debug.Log($"GetPendingOrderCount called, returning: {count}");
        return count;
    }

    public List<Order> GetAllPendingOrders()
    {
        List<Order> orders = new List<Order>();
        while (orderQueue.Count > 0)
        {
            orders.Add(orderQueue.Dequeue());
        }
        foreach (var order in orders)
        {
            orderQueue.Enqueue(order, order.GetWeight());
        }
        return orders;
    }

    public float dispatchInterval = 5f;  // 分配间隔（秒）
    public float initialDelay = 2f;  // 初始延迟，让订单先积累
    private float lastDispatchTime = 0f;

    void Update()
    {
        if (orderQueue == null) return;
        
        float currentTime = Time.time;
        // 初始延迟后才开始分配
        if (currentTime < initialDelay) return;
        
        if (orderQueue.Count > 0 && currentTime - lastDispatchTime >= dispatchInterval)
        {
            DispatchNextOrder();
            lastDispatchTime = currentTime;
        }
    }

    public void DispatchNextOrder()
    {
        if (orderQueue == null || orderQueue.Count == 0) return;
        
        Order order = orderQueue.Dequeue();
        AssignOrderToVehicle(order);
    }
}
