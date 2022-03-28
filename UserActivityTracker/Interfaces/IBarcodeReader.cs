namespace UserActivityTracker.Interfaces
{
    public interface IBarcodeReader
    {
        string[] Read(string filePath);
        bool Validate(string barcode);
    }
}
