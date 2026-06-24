using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private VehicleManager vehicleManager;
    private Dispatcher dispatcher;
    private OrderGenerator orderGenerator;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        vehicleManager = FindObjectOfType<VehicleManager>();
        dispatcher = FindObjectOfType<Dispatcher>();
        orderGenerator = FindObjectOfType<OrderGenerator>();

        if (vehicleManager == null)
        {
            Debug.LogError("VehicleManager not found!");
        }
        if (dispatcher == null)
        {
            Debug.LogError("Dispatcher not found!");
        }
        if (orderGenerator == null)
        {
            Debug.LogError("OrderGenerator not found!");
        }
    }

    public VehicleManager GetVehicleManager() => vehicleManager;
    public Dispatcher GetDispatcher() => dispatcher;
    public OrderGenerator GetOrderGenerator() => orderGenerator;
}
