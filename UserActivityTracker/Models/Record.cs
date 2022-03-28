using System;
using CsvHelper.Configuration.Attributes;

namespace UserActivityTracker.Models
{
    public class Record
    {
        //[Name("Time")]
        [Ignore]
        public string Time { get; set; }
        //[Name("Event context")]
        [Ignore]
        public string EventContext { get; set; }
        [Name("Component")]
        public string Component { get; set; }
        //[Name("Event name")]
        [Ignore]
        public string EventName { get; set; }
        //[Name("Description")]
        [Ignore]
        public string Description { get; set; }
        //[Name("Origin")]
        [Ignore]
        public string Origin { get; set; }
        [Name("IP address")]
        public string IPAddress { get; set; }

        public ActivityType Type
        {
            get
            {
                switch (Component)
                {
                    case "System":
                        return ActivityType.SYSTEM;
                    case "File":
                        return ActivityType.FILE;
                    case "Forum":
                        return ActivityType.FORUM;
                    case "Page":
                        return ActivityType.PAGE;
                    case "Activity report":
                        return ActivityType.ACTIVITYREPORT;
                    case "User report":
                        return ActivityType.USERREPORT;
                    case "Overview report":
                        return ActivityType.OVERVIEWREPORT;
                    case "Grader report":
                        return ActivityType.GRADERREPORT;
                    case "Live logs":
                        return ActivityType.LIVELOGS;
                    case "Logs":
                        return ActivityType.LOGS;
                    case "Recycle bin":
                        return ActivityType.RECYCLEBIN;
                    default:
                        throw new NotImplementedException();
                }
            }

            private set { }
        }

        public override string ToString()
        {
            return $@"Time: {Time}, Event context: {EventContext}, Component: {Component},
                    Event name: {EventName}, Description: {Description},
                    Origin: {Origin}, IP address: {IPAddress}";
        }
    }
}