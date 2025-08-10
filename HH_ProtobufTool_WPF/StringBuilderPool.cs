using System.Collections.Generic;
using System.Text;

public static class StringBuilderPool
{
    private const int MAX_POOL_SIZE = 10;
    private static readonly Stack<StringBuilder> pool = new Stack<StringBuilder>(MAX_POOL_SIZE);

    public static StringBuilder Get()
    {
        lock (pool)
        {
            if (pool.Count > 0)
            {
                return pool.Pop().Clear();
            }
        }

        return new StringBuilder(100);
    }

    public static void Return(StringBuilder sb)
    {
        if (sb == null) return;

        lock (pool)
        {
            if (pool.Count < MAX_POOL_SIZE)
            {
                sb.Clear();
                pool.Push(sb);
            }
        }
    }
}