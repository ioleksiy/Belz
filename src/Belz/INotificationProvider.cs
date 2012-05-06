using System;
using Belz.QueueProvider;

namespace Belz
{
    public interface INotificationProvider
    {
        void OnError(string message, Exception exception);
        void OnWorkerStarted(QueueMessage message);
        void OnWorkerEnded(QueueMessage message, bool success);
    }
}
