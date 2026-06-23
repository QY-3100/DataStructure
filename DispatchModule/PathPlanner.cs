using UnityEngine;
using System.Collections.Generic;

public class PathPlanner : MonoBehaviour
{
    public static PathPlanner Instance;
    public LineRenderer routeLinePrefab;
    private Dictionary<VehicleController, LineRenderer> lineDict = new Dictionary<VehicleController, LineRenderer>();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // 绘制订单路线，只绘图，不控制车辆移动（车辆移动交给VehicleController.AssignOrder）
    public void DrawOrderPath(VehicleController car, OrderData order)
    {
        if (routeLinePrefab == null) return;
        LineRenderer line;
        if (!lineDict.ContainsKey(car))
        {
            line = Instantiate(routeLinePrefab, transform);
            lineDict.Add(car, line);
        }
        else
        {
            line = lineDict[car];
        }

        line.positionCount = 3;
        line.SetPosition(0, car.transform.position);
        line.SetPosition(1, order.pickupPoint.position);
        line.SetPosition(2, order.deliveryPoint.position);

        // 加急红色，普通蓝色
        line.startColor = order.priority == OrderPriority.Urgent ? Color.red : Color.blue;
        line.endColor = order.priority == OrderPriority.Urgent ? Color.red : Color.blue;
    }
}