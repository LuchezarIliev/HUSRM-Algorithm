namespace UserActivityTracker.Interfaces
{
    public interface IDataRepository
    {
        void GetFileData(string filePath);
        void WriteToInput(string filePath);
    }
}