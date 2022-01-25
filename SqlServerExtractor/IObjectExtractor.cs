namespace SqlServerExtractor
{
    public interface IObjectExtractor
    {
        ObjectType Type { get; }
        IAsyncEnumerable<string> ListObject();
        Task<string> GetObjectDefinition(string name);
    }
}
