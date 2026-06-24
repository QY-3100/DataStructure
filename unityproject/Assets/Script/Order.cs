using UnityEngine;

public class Order
{
    public int OrderId { get; private set; }
    public Vector2 PickPoint { get; private set; }     
    public Vector2 DeliverPoint { get; private set; }  
    public PriorityLevel Priority { get; private set; }
    public long CreateTime { get; private set; }        

    private static int nextOrderId = 1;

    public Order(Vector2 pickPoint, Vector2 deliverPoint, PriorityLevel priority)
    {
        OrderId = nextOrderId++;
        PickPoint = pickPoint;
        DeliverPoint = deliverPoint;
        Priority = priority;
        CreateTime = GetCurrentTimestamp();
    }

    public float GetWeight()
    {
        int priorityWeight = (int)Priority * 10000;
        return priorityWeight + CreateTime;
    }

    private long GetCurrentTimestamp()
    {
        return (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
    }

    public override string ToString()
    {
        return $"Order[{OrderId}] - Priority:{Priority}, Pick:{PickPoint}, Deliver:{DeliverPoint}, Time:{CreateTime}";
    }
}
