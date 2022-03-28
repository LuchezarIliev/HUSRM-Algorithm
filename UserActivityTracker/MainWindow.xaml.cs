using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UserActivityTracker.Interfaces;
using UserActivityTracker.Models;

namespace UserActivityTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IBarcodeReader _barcodeReader = new BarcodeReader(new BarcodeValidator());
        private IDataRepository _dataRepository = new DataRepository(new FileReader());
        private IDataProcessingManager _dataProcessingManager = new DataProcessingManager(new DataAnalyst(new FileReader()));

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Browse_File_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".png";
            dlg.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png, *.gif) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png; *.gif";

            // Display OpenFileDialog by calling ShowDialog method 
            bool? result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                txtFilePath.Text = filename;
            }
        }

        private void Scan_Barcode_Click(object sender, RoutedEventArgs e)
        {
            string[] barcodes = _barcodeReader.Read(txtFilePath.Text);
            if (barcodes != null && barcodes.Length > 0)
                foreach (var barcode in barcodes)
                {
                    lboxBarcodes.Items.Add(barcode);
                }
            else
                MessageBox.Show("Баркод не беше намерен!");
        }

        private void Validate_Barcode_Click(object sender, RoutedEventArgs e)
        {
            var selectedBarcode = lboxBarcodes.SelectedItem;

            if (selectedBarcode != null)
            {
                if (_barcodeReader.Validate(selectedBarcode.ToString()))
                {
                    MessageBox.Show("Баркодът беше валидиран успешно!");
                    new UserInterfaceManager(_dataRepository, _dataProcessingManager).Show();
                    Hide();
                }
                else
                    MessageBox.Show("Баркодът не е валиден!");
            }
            else
                MessageBox.Show("Моля изберете някой от прочетените баркодове!");
        }
    }
}