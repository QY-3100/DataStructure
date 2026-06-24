using System.Collections.Generic;

public class OrderComparer : IComparer<Order>
{
    public int Compare(Order x, Order y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;

        float w1 = x.GetWeight();
        float w2 = y.GetWeight();

        if (w1 < w2) return -1;
        if (w1 > w2) return 1;
        return 0;
    }
}
