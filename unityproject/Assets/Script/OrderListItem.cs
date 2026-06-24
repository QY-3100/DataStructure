using UnityEngine;
using TMPro;

public class OrderListItem : MonoBehaviour
{
    public TextMeshProUGUI orderIdText;
    public TextMeshProUGUI priorityText;
    public TextMeshProUGUI createTimeText;
    public UnityEngine.UI.Image priorityIndicator;

    public Color urgentColor = Color.red;
    public Color normalColor = Color.green;

    public void SetOrder(Order order)
    {
        if (orderIdText != null)
            orderIdText.text = $"Order {order.OrderId}";

        if (priorityText != null)
            priorityText.text = order.Priority == PriorityLevel.Urgent ? "Urgent" : "Normal";

        if (priorityIndicator != null)
            priorityIndicator.color = order.Priority == PriorityLevel.Urgent ? urgentColor : normalColor;

        if (createTimeText != null)
        {
            float systemTime = TimeManager.Instance != null ? TimeManager.Instance.SystemTime : 0f;
            float minutes = ((float)systemTime - (float)order.CreateTime) / 60f;
            if (minutes < 0) minutes = 0;
            createTimeText.text = $"Wait {minutes:F1} min";
        }
    }
}
