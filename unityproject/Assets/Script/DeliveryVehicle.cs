using System.Collections.Generic;
using UnityEngine;

public interface ITask
{
    TaskType TaskType { get; }
}

public enum TaskType
{
    Delivery,
    Charge
}

public class DeliveryTask : ITask
{
    public Order Order { get; private set; }
    public TaskType TaskType { get { return TaskType.Delivery; } }

    public DeliveryTask(Order order)
    {
        Order = order;
    }
}

public class ChargeTask : ITask
{
    public Vector2 ChargeStationPosition { get; private set; }
    public TaskType TaskType { get { return TaskType.Charge; } }

    public ChargeTask(Vector2 position)
    {
        ChargeStationPosition = position;
    }
}

public class DeliveryVehicle : MonoBehaviour
{
    public int VehicleId;
    public float CurrentBattery = 100f;
    public float MaxBattery = 100f;
    public float BatteryConsumptionRate = 0.1f;
    public float BatteryThreshold = 20f;

    private LinkedList<ITask> taskList = new LinkedList<ITask>();
    private LinkedListNode<ITask> currentTaskNode;

    public bool IsBusy { get { return taskList.Count > 0; } }
    public int TaskCount { get { return taskList.Count; } }

    public void AddTaskToEnd(ITask task)
    {
        taskList.AddLast(task);
        if (currentTaskNode == null)
        {
            currentTaskNode = taskList.First;
        }
    }

    public void AddTaskToFront(ITask task)
    {
        taskList.AddFirst(task);
        if (currentTaskNode == null)
        {
            currentTaskNode = taskList.First;
        }
    }

    public void InsertTaskAfterCurrent(ITask task)
    {
        if (currentTaskNode != null)
        {
            taskList.AddAfter(currentTaskNode, task);
        }
        else
        {
            AddTaskToEnd(task);
        }
    }

    public void InsertUrgentOrderBeforeFirstNormal(Order urgentOrder)
    {
        var current = taskList.First;
        while (current != null)
        {
            if (current.Value is DeliveryTask deliveryTask && 
                deliveryTask.Order.Priority == PriorityLevel.Normal)
            {
                taskList.AddBefore(current, new DeliveryTask(urgentOrder));
                return;
            }
            current = current.Next;
        }
        AddTaskToEnd(new DeliveryTask(urgentOrder));
    }

    public ITask GetCurrentTask()
    {
        return currentTaskNode?.Value;
    }

    public void CompleteCurrentTask()
    {
        if (currentTaskNode != null)
        {
            var nextNode = currentTaskNode.Next;
            taskList.Remove(currentTaskNode);
            currentTaskNode = nextNode;
        }
    }

    public void UpdateBattery(float deltaTime)
    {
        if (IsBusy)
        {
            CurrentBattery -= BatteryConsumptionRate * deltaTime;
            CurrentBattery = Mathf.Max(0, CurrentBattery);
        }
    }

    public bool NeedsCharging()
    {
        return CurrentBattery <= BatteryThreshold;
    }

    public void Charge(float amount)
    {
        CurrentBattery = Mathf.Min(MaxBattery, CurrentBattery + amount);
    }

    public List<ITask> GetAllTasks()
    {
        List<ITask> tasks = new List<ITask>();
        var current = taskList.First;
        while (current != null)
        {
            tasks.Add(current.Value);
            current = current.Next;
        }
        return tasks;
    }

    public void ClearAllTasks()
    {
        taskList.Clear();
        currentTaskNode = null;
    }
}
