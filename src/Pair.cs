namespace Xania.ObjectMapper
{
    static class Pair
    {
        public static IPair<T1, T2> Create<T1, T2>(T1 t1, T2 t2)
        {
            return new Pair<T1, T2>(t1, t2);
        }
    }

    public class Pair<T1, T2>: IPair<T1, T2>
    {
        public Pair(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public T1 Item1 { get; }
        public T2 Item2 { get; }

        public override int GetHashCode()
        {
            return Item1?.GetHashCode() ?? 0 + Item2?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is IPair<T1, T2> other)
                return Equals(other);
            return false;
        }

        private bool Equals(IPair<T1, T2> other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Equals(other.Item1, Item1) && Equals(other.Item2, Item2);
        }
    }

    public interface IPair<out T1, out T2>
    {
        T1 Item1 { get; }
        T2 Item2 { get; }
    }
}
