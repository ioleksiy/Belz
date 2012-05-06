using System;
using System.Diagnostics;
using System.Threading;
using Belz.QueueProvider;
using Belz.QueueProvider.IronMq;
using NUnit.Framework;

namespace Belz.Tests
{
    [TestFixture]
    public class GeneralTests
    {
        private const string Token = "";
        private const string ProjectId = "";
        private const string Queue = "";

        private const int PreparedMessages = 500;

        [Test(Description = "General Test")]
        public void GeneralTest()
        {
            Print("Started");
            using (var q = new BelzProviderIronMq(Token, ProjectId, Queue))
            {
                var a = new QueueMessage[PreparedMessages];
                for (var i = 0; i < PreparedMessages; i++)
                {
                    a[i] = new QueueMessage("d").WithParameter("m", "Hello " + i);
                }
                q.Enqueue(a);

                Print("Enqueued");

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                using (var belz = new Belz(q))
                {
                    belz.Error += BelzOnError;

                    belz.OperationalThreadsCount = 30;
                    belz.RegisterWorker<Worker>("d");
                    belz.Run();
                    while (q.AvailableMessages > 0)
                    {
                        Thread.Sleep(500);
                    }

                    belz.Error -= BelzOnError;
                }
                stopWatch.Stop();

                var ts = stopWatch.Elapsed;
                var elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                Print("RunTime " + elapsedTime);
                Print("Requests to IronMq made " + q.NumberOfRequestsMade);
            }
            Print("Ended");
        }

        private static void BelzOnError(string message, Exception exception)
        {
            Print("Error - `" + message + "`: " + exception);
        }

        private static void Print(string msg)
        {
            Debug.WriteLine(DateTime.Now.ToFileTime() + ": " + msg);
        }

        protected class Worker : BeltzWorker
        {
            protected override void ExecuteNode(QueueMessage message)
            {
                var s = message.Parameters["m"];
                Thread.CurrentThread.Name = "Worker " + s;
                Print("Launched - " + s);
                Print("Status - " + s + " = " + TimeoutLeft());
                Thread.Sleep(TimeoutLeft(1000));
                if (TimeHasPassed)
                {
                    Print("Aborted - " + s);
                    throw new Exception("Failed try " + s);
                }
                Print("Ended - " + s);
            }
        }
    }
}
