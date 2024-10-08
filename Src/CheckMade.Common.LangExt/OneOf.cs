using System.Diagnostics.CodeAnalysis;

namespace CheckMade.Common.LangExt;

public class OneOf<T1>
{
    public static implicit operator OneOf<T1>(T1 value) => new(value);

    private readonly T1? _value;

    public OneOf(T1 value) => _value = value;

    protected OneOf() { }

    public bool Is<T>([MaybeNullWhen(false)] out T value)
    {
        value = default;
        if (_value is T cast)
        {
            value = cast;
            return true;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is OneOf<T1> other)
        {
            return EqualityComparer<T1?>.Default.Equals(_value, other._value);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return _value != null 
            ? EqualityComparer<T1?>.Default.GetHashCode(_value) 
            : 0;
    }
}

public class OneOf<T1, T2> : OneOf<T1>
{
    public static implicit operator OneOf<T1, T2>(T1 value) => new(value);
    public static implicit operator OneOf<T1, T2>(T2 value) => new(value);

    private readonly T2? _value;

    public OneOf(T1 value) : base(value) { }
    public OneOf(T2 value) => _value = value;
    protected OneOf() { }

    public new bool Is<T>([MaybeNullWhen(false)] out T value)
    {
        if (base.Is(out value)) return true;

        if (_value is T cast)
        {
            value = cast;
            return true;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is OneOf<T1, T2> other)
        {
            return base.Equals(obj) && EqualityComparer<T2?>.Default.Equals(_value, other._value);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), _value != null 
            ? EqualityComparer<T2?>.Default.GetHashCode(_value) 
            : 0);
    }
}

public class OneOf<T1, T2, T3> : OneOf<T1, T2>
{
    public static implicit operator OneOf<T1, T2, T3>(T1 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3>(T2 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3>(T3 value) => new(value);

    private readonly T3? _value;

    public OneOf(T1 value) : base(value) { }
    public OneOf(T2 value) : base(value) { }
    public OneOf(T3 value) => _value = value;

    public new bool Is<T>([MaybeNullWhen(false)] out T value)
    {
        if (base.Is(out value)) return true;

        if (_value is T cast)
        {
            value = cast;
            return true;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is OneOf<T1, T2, T3> other)
        {
            return base.Equals(obj) && EqualityComparer<T3?>.Default.Equals(_value, other._value);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), _value != null 
            ? EqualityComparer<T3?>.Default.GetHashCode(_value) 
            : 0);
    }
}
