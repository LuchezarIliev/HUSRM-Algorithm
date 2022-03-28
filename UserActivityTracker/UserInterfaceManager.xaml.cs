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
using System.Windows.Shapes;
using UserActivityTracker.Interfaces;

namespace UserActivityTracker
{
    /// <summary>
    /// Interaction logic for UserInterfaceManager.xaml
    /// </summary>
    public partial class UserInterfaceManager : Window
    {
        private IDataRepository _dataRepository;
        private IDataProcessingManager _dataProcessingManager;

        public UserInterfaceManager(IDataRepository dataRepository, IDataProcessingManager dataProcessingManager)
        {
            InitializeComponent();
            _dataRepository = dataRepository;
            _dataProcessingManager = dataProcessingManager;
        }

        private void ProcessData(object sender, RoutedEventArgs e)
        {
            // THIS IS THE INPUT FILE:
            string input = inputFileTextBox.Text;
            // THIS IS THE OUTPUT FILE PATH FOR SAVING HIGH UTILITY SEQUENTIAL RULES
            string output = outputFileTextBox.Text;

            if (input.Equals(string.Empty) || output.Equals(string.Empty))
            {
                MessageBox.Show("Трабва да изберете входен и изходен файл за алгоритъма!");
                return;
            }

            if (!input.EndsWith(".txt") || !output.EndsWith(".txt"))
            {
                MessageBox.Show("Входният и изходният файл трябва да бъдат с разширение .txt!");
                return;
            }

            // THIS IS THE MINIMUM CONFIDENCE PARAMETER  (e.g. 70 %)
            double minConf;
            if (!double.TryParse(minConfTextBox.Text, out minConf))
                MessageBox.Show("Невалиден параметър!");
            minConf = minConf / 100;

            // THIS IS THE MINIMUM UTILITY PARAMETER  (e.g. 30 $ ) 
            double minUtil;
            if (!double.TryParse(minUtilTextBox.Text, out minUtil))
                MessageBox.Show("Невалиден параметър!");

            //  THESE ARE ADDITIONAL PARAMETERS
            //   THE FIRST PARAMETER IS A CONSTRAINT ON THE MAXIMUM NUMBER OF ITEMS IN THE LEFT SIDE OF RULES
            // For example, we don't want to find rules with more than 4 items in their left side
            int maxAntecedentSize;
            if (!int.TryParse(maxAntSizeTextBox.Text, out maxAntecedentSize))
                MessageBox.Show("Невалиден параметър!");
            //   THE SECOND PARAMETER IS A CONSTRAINT ON THE MAXIMUM NUMBER OF ITEMS IN THE RIGHT SIDE OF RULES
            // For example, we don't want to find rules with more than 4 items in their right side
            int maxConsequentSize;
            if (!int.TryParse(maxConsSizeTextBox.Text, out maxConsequentSize))
                MessageBox.Show("Невалиден параметър!");

            // This parameter let the user specify how many sequences from the input file should be used.
            // For example, it could be used to read only the first 1000 sequences of an input file
            int maximumSequenceCount = int.MaxValue;
            if (!int.TryParse(maxSeqCountTextBox.Text, out maximumSequenceCount))
                MessageBox.Show("Невалиден параметър!");

            try
            {
                _dataProcessingManager.RunAlgorithm(input, output, minConf, minUtil, maxAntecedentSize, maxConsequentSize, maximumSequenceCount);
                MessageBox.Show($"Успешен запис във файла: {output}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Възникна грешка!\n{ex.Message}");
            }
        }

        private void ReadInputFile_Click(object sender, RoutedEventArgs e)
        {
            inputFileTextBox.Text = GetFileNameDialog("Text files (*.txt) | *.txt;");
        }

        private void ReadOutputFile_Click(object sender, RoutedEventArgs e)
        {
            outputFileTextBox.Text = GetFolderNameDialog();
        }

        private string GetFileNameDialog(string filter)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            //dlg.DefaultExt = ".png";
            dlg.Filter = filter;

            // Display OpenFileDialog by calling ShowDialog method 
            bool? result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                return dlg.FileName;
            }

            return "";
        }

        private void ReadCsvFile_Click(object sender, RoutedEventArgs e)
        {
            csvTextBox.Text = GetFileNameDialog("Comma-separated values (*.csv) | *.csv;");
        }

        private string GetFolderNameDialog()
        {
            // Create a "Save As" dialog for selecting a directory (HACK)
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Title = "Select a Directory"; // instead of default "Save As"
            dialog.Filter = "txt|*.txt"; // Prevents displaying files
            dialog.FileName = "Input"; // Filename will then be "select.this.directory"

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string path = dialog.FileName;
                // Remove fake filename from resulting path
                path = path.Replace("\\select.this.directory", "");
                path = path.Replace(".this.directory", "");
                // Our final value is in path
                return path;
            }

            return "";
        }

        private void ReadFileToInput_Click(object sender, RoutedEventArgs e)
        {
            saveInputTextBox.Text = GetFolderNameDialog();
        }

        private void FilterData(object sender, RoutedEventArgs e)
        {
            if (csvTextBox.Text.Equals(string.Empty))
            {
                MessageBox.Show("Пътят към .csv файла не може да бъде празен!");
                return;
            }
            try
            {
                _dataRepository.GetFileData(csvTextBox.Text);
                MessageBox.Show("Трансформирането на данните беше успешно!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Възникна грешка!\n{ex.Message}");
            }
        }

        private void SaveDataToInput(object sender, RoutedEventArgs e)
        {
            if (saveInputTextBox.Text.Equals(string.Empty))
            {
                MessageBox.Show("Пътят към файла за запис не може да бъде празен!");
                return;
            }
            try
            {
                _dataRepository.WriteToInput(saveInputTextBox.Text);
                MessageBox.Show($"Трансформираните данни бяха записани във файла: {saveInputTextBox.Text}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Възникна грешка!\n{ex.Message}");
            }
        }

        private void ReadResultsFile_Click(object sender, RoutedEventArgs e)
        {
            resultsTextBox.Text = GetFileNameDialog("Text files (*.txt) | *.txt;");
        }

        private void DisplayResults(object sender, RoutedEventArgs e)
        {
            try
            {
                var results = _dataProcessingManager.GetResults(resultsTextBox.Text);
                MessageBox.Show(results, "Оценка на резултатите");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Възникна грешка!\n{ex.Message}");
            }
        }
    }
}