using UnityEngine;

public enum VehicleState
{
    Idle,
    MovingToPickup,
    MovingToDelivery,
    NeedCharging,
    Charging
}

public class VehicleController : MonoBehaviour
{
    public int vehicleId;
    public float battery = 100f;
    public float maxBattery = 100f;
    public float load = 0f;
    public float maxLoad = 10f;
    public float moveSpeed = 10f;
    public bool isBusy = false;
    public VehicleState state = VehicleState.Idle;

    private Vector3 targetPosition;
    private bool hasTarget = false;

    private OrderData currentOrder;

    void Update()
    {
        if (hasTarget)
        {
            MoveStep();
        }
    }

    public void AssignOrder(OrderData order)
    {
        currentOrder = order;
        isBusy = true;
        state = VehicleState.MovingToPickup;

        MoveTo(order.pickupPoint.position);

        Debug.Log("车辆 " + vehicleId + " 接到订单 " + order.orderId + "，正在前往取货点");
    }

    public void MoveTo(Vector3 target)
    {
        targetPosition = target;
        hasTarget = true;
    }

    private void MoveStep()
    {
        Vector3 oldPosition = transform.position;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        float distanceThisFrame = Vector3.Distance(oldPosition, transform.position);
        battery -= distanceThisFrame * 0.05f;

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            ArriveTarget();
        }
    }

    private void ArriveTarget()
    {
        hasTarget = false;

        if (battery <= 20f)
        {
            state = VehicleState.NeedCharging;
            Debug.Log("车辆 " + vehicleId + " 电量不足，需要充电");
            return;
        }

        if (currentOrder == null)
        {
            isBusy = false;
            state = VehicleState.Idle;
            return;
        }

        if (state == VehicleState.MovingToPickup)
        {
            state = VehicleState.MovingToDelivery;
            MoveTo(currentOrder.deliveryPoint.position);

            Debug.Log("车辆 " + vehicleId + " 已到达取货点，正在前往送货点");
        }
        else if (state == VehicleState.MovingToDelivery)
        {
            currentOrder.isFinished = true;
            Debug.Log("车辆 " + vehicleId + " 完成订单 " + currentOrder.orderId);

            currentOrder = null;
            isBusy = false;
            state = VehicleState.Idle;
        }
    }
}