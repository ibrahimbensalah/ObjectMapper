namespace Xania.ObjectMapper
{
    public interface IMap<in TSource, out TTarget>
    {
        IOption<TTarget> this[TSource name] { get; }
    }
}