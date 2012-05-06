using System;
using System.Threading;
using Belz.QueueProvider;

namespace Belz
{
    public abstract class BeltzWorker : IDisposable
    {
        private DateTime _watch;
        private int _timeout;

        public void Execute(IQueueProvider queueProvider, QueueMessage message)
        {
            _watch = DateTime.UtcNow;
            _timeout = message.Timeout;
            Provider = queueProvider;
            ExecuteNode(message);
        }

        protected int TimeoutLeft(int millisecondsNeeded = Timeout.Infinite)
        {
            if (_timeout < 1)
                return millisecondsNeeded;
            var t = (int)(_timeout - (DateTime.UtcNow - _watch).TotalMilliseconds);
            if (t < 0)
                t = 0;
            if (millisecondsNeeded != Timeout.Infinite)
                return t < millisecondsNeeded ? t : millisecondsNeeded;
            return t;
        }

        protected bool TimeHasPassed
        {
            get { return TimeoutLeft() == 0; }
        }

        protected IQueueProvider Provider { get; private set; }

        protected abstract void ExecuteNode(QueueMessage message);

        public virtual void Dispose() { }
    }
}
