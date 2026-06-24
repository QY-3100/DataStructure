using System.Collections.Generic;
using UnityEngine;

public class OrderGenerator : MonoBehaviour
{
    public float generateInterval = 2f;
    public float mapWidth = 50f;
    public float mapHeight = 50f;
    public float urgentProbability = 0.3f;

    private float timer = 0f;

    public delegate void OrderGenerated(Order order);
    public event OrderGenerated OnOrderGenerated;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= generateInterval)
        {
            timer = 0;
            GenerateOrder();
        }
    }

    void GenerateOrder()
    {
        Vector2 pickPoint = new Vector2(
            Random.Range(-mapWidth / 2, mapWidth / 2),
            Random.Range(-mapHeight / 2, mapHeight / 2)
        );

        Vector2 deliverPoint = new Vector2(
            Random.Range(-mapWidth / 2, mapWidth / 2),
            Random.Range(-mapHeight / 2, mapHeight / 2)
        );

        PriorityLevel priority = Random.value < urgentProbability ? PriorityLevel.Urgent : PriorityLevel.Normal;

        Order order = new Order(pickPoint, deliverPoint, priority);

        OnOrderGenerated?.Invoke(order);
        Debug.Log($"Generated: {order}");
    }

    public void GenerateOrderManually()
    {
        GenerateOrder();
    }
}
