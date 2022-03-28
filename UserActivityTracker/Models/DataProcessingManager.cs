using UserActivityTracker.Interfaces;
using HUSRM;

namespace UserActivityTracker.Models
{
    internal class DataProcessingManager : IDataProcessingManager
    {
        private IDataAnalyst _dataAnalyst;

        public DataProcessingManager(IDataAnalyst dataAnalyst)
        {
            _dataAnalyst = dataAnalyst;
        }

        public void RunAlgorithm(string input, string output, double minConf, double minUtil, int maxAntecedentSize, int maxConsequentSize, int maximumSequenceCount)
        {
            // This create the algorithm and runs it
            // Results will be stored to the file output
            AlgoHUSRM algo = new AlgoHUSRM();
            algo.runAlgorithm(input, output, minConf, minUtil, maxAntecedentSize, maxConsequentSize, maximumSequenceCount);
        }

        public string GetResults(string filePath)
        {
            return _dataAnalyst.GetOutput(filePath);
        }
    }
}