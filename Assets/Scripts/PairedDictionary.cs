using System.Collections.Generic;

public class PairedDictionary<T1, T2>
{
    readonly Dictionary<T1, T2> a;
    readonly Dictionary<T2, T1> b;

    public PairedDictionary()
    {
        a = new Dictionary<T1, T2>();
        b = new Dictionary<T2, T1>();
    }

    public void Add(T1 valueA, T2 valueB)
    {
        a.Add(valueA, valueB);
        b.Add(valueB, valueA);
    }

    public void Add(T2 valueB, T1 valueA)
    {
        a.Add(valueA, valueB);
        b.Add(valueB, valueA);
    }

    public T1 Get(T2 key)
    {
        return b[key];
    }

    public T2 Get(T1 key)
    {
        return a[key];
    }

    public void Set(T1 valueA, T2 valueB)
    {
        this.a[valueA] = valueB;
        this.b[valueB] = valueA;
    }

    public void Remove(T1 key)
    {
        b.Remove(Get(key));
        a.Remove(key);
    }

    public T1 this[T2 key]
    {
        get { return Get(key);  }
        set { Set(value, key); }
    }

    public T2 this[T1 key]
    {
        get { return Get(key); }
        set { Set(key, value); }
    }

    public void Remove(T2 key)
    {
        a.Remove(Get(key));
        b.Remove(key);
    }

    public Dictionary<T1, T2> GetDictionaryA()
    {
        return a;
    }

    public Dictionary<T2, T1> GetDictionaryB()
    {
        return b;
    }
}