using System.Collections.Generic;
using UnityEngine;

public class VehicleManager : MonoBehaviour
{
    public int vehicleCount = 5;
    public GameObject vehiclePrefab;
    public Transform spawnArea;

    private Dictionary<int, DeliveryVehicle> vehicles = new Dictionary<int, DeliveryVehicle>();

    void Start()
    {
        InitializeVehicles();
    }

    void InitializeVehicles()
    {
        if (vehiclePrefab == null)
        {
            Debug.LogError("Vehicle prefab is not assigned!");
            return;
        }

        for (int i = 1; i <= vehicleCount; i++)
        {
            Vector3 spawnPosition = spawnArea != null 
                ? new Vector3(
                    Random.Range(spawnArea.position.x - 5, spawnArea.position.x + 5),
                    0.5f,
                    Random.Range(spawnArea.position.z - 5, spawnArea.position.z + 5)
                )
                : new Vector3(Random.Range(-10, 10), 0.5f, Random.Range(-10, 10));

            GameObject vehicleObj = Instantiate(vehiclePrefab, spawnPosition, Quaternion.identity);
            vehicleObj.name = $"Car{i}";

            DeliveryVehicle vehicle = vehicleObj.GetComponent<DeliveryVehicle>();
            if (vehicle == null)
            {
                vehicle = vehicleObj.AddComponent<DeliveryVehicle>();
            }
            vehicle.VehicleId = i;

            vehicles.Add(i, vehicle);
            Debug.Log($"Vehicle {i} initialized at {spawnPosition}");
        }
    }

    public DeliveryVehicle GetVehicle(int vehicleId)
    {
        if (vehicles.ContainsKey(vehicleId))
        {
            return vehicles[vehicleId];
        }
        return null;
    }

    public List<DeliveryVehicle> GetVehicles()
    {
        return new List<DeliveryVehicle>(vehicles.Values);
    }

    public int GetVehicleCount()
    {
        return vehicles.Count;
    }

    public void UpdateVehicles(float deltaTime)
    {
        foreach (var vehicle in vehicles.Values)
        {
            vehicle.UpdateBattery(deltaTime);
        }
    }

    void Update()
    {
        UpdateVehicles(Time.deltaTime);
    }
}
