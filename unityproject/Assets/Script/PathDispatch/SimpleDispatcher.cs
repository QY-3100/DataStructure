using UnityEngine;

public class SimpleDispatcher : MonoBehaviour
{
    public VehicleController[] vehicles;

    public Transform pickupPoint;
    public Transform deliveryPoint;

    private int orderIdCounter = 1;

    void Start()
    {
        CreateAndDispatchOrder();
    }

    public void CreateAndDispatchOrder()
    {
        OrderData order = new OrderData(
            orderIdCounter,
            pickupPoint,
            deliveryPoint,
            OrderPriority.Normal
        );

        orderIdCounter++;

        VehicleController bestVehicle = FindNearestIdleVehicle(pickupPoint.position);

        if (bestVehicle == null)
        {
            Debug.LogWarning("没有空闲车辆，订单暂时无法分配");
            return;
        }

        bestVehicle.AssignOrder(order);

        Debug.Log("订单 " + order.orderId + " 分配给车辆 " + bestVehicle.vehicleId);
    }

    private VehicleController FindNearestIdleVehicle(Vector3 targetPosition)
    {
        VehicleController bestVehicle = null;
        float bestDistance = float.MaxValue;

        foreach (VehicleController vehicle in vehicles)
        {
            if (vehicle == null)
            {
                continue;
            }

            if (vehicle.isBusy)
            {
                continue;
            }

            float distance = Vector3.Distance(vehicle.transform.position, targetPosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestVehicle = vehicle;
            }
        }

        return bestVehicle;
    }
}