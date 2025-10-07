namespace ComicReader.Core.Abstractions
{
    public interface ISettingsService
    {
        string Theme { get; }
        void Save();
    }
}
