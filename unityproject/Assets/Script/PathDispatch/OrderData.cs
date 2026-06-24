using UnityEngine;

public enum OrderPriority
{
    Urgent,
    Normal
}

public class OrderData
{
    public int orderId;
    public Transform pickupPoint;
    public Transform deliveryPoint;
    public OrderPriority priority;
    public bool isFinished;

    public OrderData(int id, Transform pickup, Transform delivery, OrderPriority orderPriority)
    {
        orderId = id;
        pickupPoint = pickup;
        deliveryPoint = delivery;
        priority = orderPriority;
        isFinished = false;
    }
}