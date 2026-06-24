using UnityEngine;

public class VehicleMoveTest : MonoBehaviour
{
    public VehicleController testVehicle;
    public Transform targetPoint;

    void Start()
    {
        if (testVehicle != null && targetPoint != null)
        {
            testVehicle.MoveTo(targetPoint.position);
        }
    }
}