using UserActivityTracker.Interfaces;
using OnBarcode.Barcode.BarcodeScanner;

namespace UserActivityTracker.Models
{
    internal class BarcodeReader : IBarcodeReader
    {
        private IBarcodeValidator _barcodeValidator;

        public BarcodeReader(IBarcodeValidator barcodeValidator)
        {
            _barcodeValidator = barcodeValidator;
        }

        public string[] Read(string filePath)
        {
            return BarcodeScanner.Scan(filePath, BarcodeType.All);
        }

        public bool Validate(string barcode)
        {
            return _barcodeValidator.IsValidBarcode(barcode);
        }
    }
}