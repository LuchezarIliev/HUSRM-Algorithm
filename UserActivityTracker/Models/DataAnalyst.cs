using System;
using System.Text;
using System.Globalization;
using UserActivityTracker.Interfaces;

namespace UserActivityTracker.Models
{
    internal class DataAnalyst : IDataAnalyst
    {
        private IFileReader _fileReader;

        public DataAnalyst(IFileReader fileReader)
        {
            _fileReader = fileReader;
        }

        public string GetOutput(string filePath)
        {
            StringBuilder results = new StringBuilder();
            using (var reader = _fileReader.Read(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var analyzedLine = AnalyzeLine(line);
                    results.Append(analyzedLine);
                }
                
            }
            return results.ToString();
        }

        private string AnalyzeLine(string line)
        {
            string[] arr = line.Split('\t');

            string activitiesDone = GetActivitiesString(arr[0]);
            string activitiesToBeDone = GetActivitiesString(arr[1].Replace("==> ", ""));

            int support = int.Parse(arr[2].Replace("#SUP: ", ""));
            double confidence = double.Parse(arr[3].Replace("#CONF: ", ""));
            int utility = int.Parse(arr[4].Replace("#UTIL: ", ""));
            
            return $@"Всички потребители, които извършват активностите: [{activitiesDone}], ще извършат и активностите: [{activitiesToBeDone}]
                с увереност: {confidence.ToString("P", CultureInfo.InvariantCulture)}, като това правило е генерирало брой кликове: {utility} 
                и се проявява в {support} последователности.{Environment.NewLine}";
        }

        private string GetActivitiesString(string activitiesLine)
        {
            StringBuilder sb = new StringBuilder();
            string[] activities = activitiesLine.Split(',');
            bool first = true;

            foreach (var a in activities)
            {
                if (first)
                    first = false;
                else
                    sb.Append(',');
                    
                switch ((ActivityType)(int.Parse(a) - 1))
                {
                    case ActivityType.SYSTEM: sb.Append("System"); break;
                    case ActivityType.FILE: sb.Append("File"); break;
                    case ActivityType.FORUM: sb.Append("Forum"); break;
                    case ActivityType.PAGE: sb.Append("Page"); break;
                    case ActivityType.ACTIVITYREPORT: sb.Append("Activity report"); break;
                    case ActivityType.USERREPORT: sb.Append("User report"); break;
                    case ActivityType.OVERVIEWREPORT: sb.Append("Overview report"); break;
                    case ActivityType.GRADERREPORT: sb.Append("Grader report"); break;
                    case ActivityType.LIVELOGS: sb.Append("Live logs"); break;
                    case ActivityType.LOGS: sb.Append("Logs"); break;
                    case ActivityType.RECYCLEBIN: sb.Append("Recycle bin"); break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return sb.ToString();
        }
    }
}