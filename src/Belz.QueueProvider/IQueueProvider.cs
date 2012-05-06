using System;
using System.Collections.Generic;

namespace Belz.QueueProvider
{
    public interface IQueueProvider : IDisposable
    {
        IEnumerable<QueueMessage> Retrieve(int messagesCount = 1);
        bool Remove(QueueMessage message);
        void Enqueue(params QueueMessage[] messages);
        int AvailableMessages { get; }
        int NumberOfRequestsMade { get; }
    }
}
