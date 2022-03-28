using System.Linq;
using UserActivityTracker.Interfaces;

namespace UserActivityTracker.Models
{
    internal class BarcodeValidator : IBarcodeValidator
    {
        private string[] ValidBarcodeCollection = new string[] { "12345670", "A982/624833B", "Patch 2" };

        public bool IsValidBarcode(string barcode)
        {
            return ValidBarcodeCollection.Contains(barcode);
        }
    }
}