using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loucengqiehuan : MonoBehaviour
{
    public Transform modelTransform;
    public Vector3 outPosition;
    public Vector3 originalPosition;
    public float moveSpeed = 2f; // 控制移动速度
    private bool isMovingOut = false; // 记录模型是否正在移动出来的状态
    private bool hasMovedOut = false; // 记录模型是否已经移动出来过

    void Start()
    {
        // 保存模型的原始位置
        originalPosition = modelTransform.position;
    }

    void Update()
    {
        if (isMovingOut && !hasMovedOut)
        {
            // 如果模型正在移动出来且尚未移动出来过，每帧向外移动一小步
            modelTransform.position = Vector3.MoveTowards(modelTransform.position, outPosition, moveSpeed * Time.deltaTime);

            // 如果模型已经到达目标位置，停止移动并将状态设置为已移动出来
            if (modelTransform.position == outPosition)
            {
                hasMovedOut = true;
            }
        }
        else if (!isMovingOut && hasMovedOut)
        {
            // 如果模型不在移动出来的状态且已经移动出来过，检查是否需要移回原位
            modelTransform.position = Vector3.MoveTowards(modelTransform.position, originalPosition, moveSpeed * Time.deltaTime);

            // 如果模型已经回到原位，将状态设置为未移动出来
            if (modelTransform.position == originalPosition)
            {
                hasMovedOut = false;
            }
        }
    }

    public void ToggleMove()
    {
        // 切换模型的移动状态
        isMovingOut = !isMovingOut;
    }
}