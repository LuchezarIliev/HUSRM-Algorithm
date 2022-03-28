namespace UserActivityTracker.Interfaces
{
    public interface IDataProcessingManager
    {
        void RunAlgorithm(string input,
            string output,
            double minConf,
            double minUtil,
            int maxAntecedentSize,
            int maxConsequentSize,
            int maximumSequenceCount);
        string GetResults(string filePath);
    }
}
