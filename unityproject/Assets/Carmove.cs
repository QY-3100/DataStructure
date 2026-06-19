using System.Collections.Generic;
using UnityEngine;

public class CarMove : MonoBehaviour
{
    public List<Transform> path;
    public float speed = 3f;

    int index = 0;

    void Update()
    {
        if (path == null || path.Count == 0) return;

        Transform target = path[index];

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            index++;
            if (index >= path.Count) index = path.Count - 1;
        }
    }
}