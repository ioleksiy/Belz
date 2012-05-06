using System;
using System.Threading;
using Belz.QueueProvider;

namespace Belz
{
    internal class WorkerExecutor
    {
        public static void Execute(INotificationProvider notifications, Type workerType, QueueMessage message, IQueueProvider provider)
        {
            notifications.OnWorkerStarted(message);
            new Thread(x => ExecuteAsync(notifications, workerType, message, provider)).Start();
        }

        private static void ExecuteAsync(INotificationProvider notifications, Type workerType, QueueMessage message, IQueueProvider provider)
        {
            BeltzWorker worker = null;
            var success = false;
            try
            {
                worker = (BeltzWorker)Activator.CreateInstance(workerType);
                worker.Execute(provider, message);
                success = true;
            }
            catch (Exception e)
            {
                notifications.OnError("Error executing worker", e);
                success = false;
            }
            finally
            {
                if (worker != null)
                    worker.Dispose();
                notifications.OnWorkerEnded(message, success);
            }
        }
    }
}
