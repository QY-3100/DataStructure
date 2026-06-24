using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class OrderMonitorUI : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI systemTimeText;

    private Dispatcher dispatcher;
    private TimeManager timeManager;

    void Start()
    {
        dispatcher = FindObjectOfType<Dispatcher>();
        timeManager = FindObjectOfType<TimeManager>();
    }

    void Update()
    {
        UpdateSystemTime();
        UpdateUI();  // 每帧更新UI
    }

    public void UpdateUI(Order updatedOrder = null)
    {
        if (dispatcher == null || statusText == null) return;

        int total = dispatcher.GetPendingOrderCount();
        var pendingOrders = dispatcher.GetAllPendingOrders();
        
        int urgentCount = 0;
        int normalCount = 0;

        foreach (var order in pendingOrders)
        {
            if (order.Priority == PriorityLevel.Urgent)
                urgentCount++;
            else
                normalCount++;
        }

        statusText.text = $"Pending: {total} | Urgent: {urgentCount} | Normal: {normalCount}";
    }

    void UpdateSystemTime()
    {
        if (timeManager != null && systemTimeText != null)
        {
            float systemMinutes = timeManager.SystemTime / 60f;
            int hours = Mathf.FloorToInt(systemMinutes / 60f);
            int minutes = Mathf.FloorToInt(systemMinutes % 60f);
            systemTimeText.text = $"System Time: {hours:00}:{minutes:00}";
        }
    }
}
