using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Timers;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>("http://+:8080/"))
            {
                Console.WriteLine("Server running at http://+:8080/");
                Console.ReadLine();
            }
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR<MyConnection>("/signalr");

            var context = GlobalHost.ConnectionManager.GetConnectionContext<MyConnection>();

            var minuteActivity = Guid.NewGuid();
            var tenActivity = Guid.NewGuid();
            var start = DateTime.UtcNow;
            var lastMinute = -1;
            var lastTen = -1;
            
            var timer = new Timer();
            timer.Elapsed += (sender, e) =>
            {
                var elapsed = e.SignalTime.Subtract(start);
                
                var currentMinute = (int)Math.Floor(elapsed.TotalMinutes);
                var currentSecond = (int)Math.Floor(elapsed.TotalSeconds);
                var currentTen = currentSecond / 10;
                var remainder = currentSecond % 10;

                if (currentTen != lastTen)
                {
                    if (lastTen > -1)
                    {
                        context.Connection.Broadcast(new ProgressRecord
                        {
                            ActivityId = tenActivity,
                            ParentActivityId = minuteActivity,
                            PercentComplete = 100,
                            SecondsRemaining = 0,
                            RecordType = "Complete",
                            Activity = $"Ten: {lastTen}",
                            StatusDescription = $"Ten: {lastTen}",
                        });
                    }

                    context.Connection.Broadcast(new ProgressRecord
                    {
                        ActivityId = minuteActivity,
                        ParentActivityId = Guid.Empty,
                        PercentComplete = (int)((elapsed.TotalMinutes - currentMinute) * 100),
                        SecondsRemaining = (int)((1 - (elapsed.TotalMinutes - currentMinute)) * 60),
                        RecordType = "Progress",
                        Activity = $"Minute: {currentMinute}",
                        StatusDescription = $"Minute: {currentMinute}",
                    });

                    tenActivity = Guid.NewGuid();
                    lastTen = currentTen;
                }
                
                if (currentMinute != lastMinute)
                {
                    if (lastMinute > -1)
                    {
                        context.Connection.Broadcast(new ProgressRecord
                        {
                            ActivityId = minuteActivity,
                            ParentActivityId = Guid.Empty,
                            PercentComplete = 100,
                            SecondsRemaining = 0,
                            RecordType = "Complete",
                            Activity = $"Minute: {lastMinute}",
                            StatusDescription = $"Minute: {lastMinute}",
                        });
                    }

                    minuteActivity = Guid.NewGuid();
                    lastMinute = currentMinute;

                    context.Connection.Broadcast(new ProgressRecord
                    {
                        ActivityId = minuteActivity,
                        ParentActivityId = Guid.Empty,
                        PercentComplete = (int)((elapsed.TotalMinutes - currentMinute) * 100),
                        SecondsRemaining = (int)((1 - (elapsed.TotalMinutes - currentMinute)) * 60),
                        RecordType = "Progress",
                        Activity = $"Minute: {currentMinute}",
                        StatusDescription = $"Minute: {currentMinute}",
                    });
                }
                
                context.Connection.Broadcast(new ProgressRecord
                {
                    ActivityId = tenActivity,
                    ParentActivityId = minuteActivity,
                    PercentComplete = remainder * 10,
                    SecondsRemaining = 10 - remainder,
                    RecordType = "Progress",
                    Activity = $"Ten: {currentTen}",
                    StatusDescription = $"Ten: {currentTen}",
                });
            };
            timer.Interval = 1000;
            
            timer.Start();            
        }
        
    }
    
    public class MyConnection : PersistentConnection
    {
    }

    public class ProgressRecord
    {
        public Guid ActivityId { get; set; }

        public Guid ParentActivityId { get; set; }

        public string RecordType { get; set; }

        public string Activity { get; set; }

        public string StatusDescription { get; set; }

        public int SecondsRemaining { get; set; }

        public int PercentComplete { get; set; }
    }
}
