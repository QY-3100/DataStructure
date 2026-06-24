using System.Collections.Generic;

public class SimplePriorityQueue<T>
{
    private List<(T item, float priority)> items = new List<(T, float)>();

    public int Count => items.Count;

    public void Enqueue(T item, float priority)
    {
        items.Add((item, priority));
        int childIndex = items.Count - 1;
        
        while (childIndex > 0)
        {
            int parentIndex = (childIndex - 1) / 2;
            if (items[childIndex].priority >= items[parentIndex].priority)
                break;
            
            Swap(childIndex, parentIndex);
            childIndex = parentIndex;
        }
    }

    public T Dequeue()
    {
        int lastIndex = items.Count - 1;
        T frontItem = items[0].item;
        items[0] = items[lastIndex];
        items.RemoveAt(lastIndex);
        lastIndex--;

        int parentIndex = 0;
        while (true)
        {
            int leftChildIndex = parentIndex * 2 + 1;
            int rightChildIndex = parentIndex * 2 + 2;
            int smallestIndex = parentIndex;

            if (leftChildIndex <= lastIndex && items[leftChildIndex].priority < items[smallestIndex].priority)
                smallestIndex = leftChildIndex;

            if (rightChildIndex <= lastIndex && items[rightChildIndex].priority < items[smallestIndex].priority)
                smallestIndex = rightChildIndex;

            if (smallestIndex == parentIndex)
                break;

            Swap(parentIndex, smallestIndex);
            parentIndex = smallestIndex;
        }

        return frontItem;
    }

    public T Peek()
    {
        return items[0].item;
    }

    private void Swap(int i, int j)
    {
        (T, float) temp = items[i];
        items[i] = items[j];
        items[j] = temp;
    }
}
