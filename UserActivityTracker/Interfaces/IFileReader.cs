using System.IO;

namespace UserActivityTracker.Interfaces
{
    public interface IFileReader
    {
        StreamReader Read(string filePath);
    }
}
