using System.IO;
using System.Text;
using UserActivityTracker.Interfaces;

namespace UserActivityTracker.Models
{
    internal class FileReader : IFileReader
    {
        public StreamReader Read(string filePath)
        {
            return new StreamReader(filePath, Encoding.GetEncoding(1251));
        }
    }
}