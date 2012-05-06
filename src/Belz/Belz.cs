using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Belz.QueueProvider;

namespace Belz
{
    public sealed class Belz : INotificationProvider, IDisposable
    {
        private readonly IQueueProvider _queueProvider;
        private readonly IDictionary<string, Type> _workers = new Dictionary<string, Type>();

        public event Action<string, Exception> Error;

        public Belz(IQueueProvider queueProvider)
        {
            _queueProvider = queueProvider;
            QueueCheckFrequency = 60;
            OperationalThreadsCount = 5;
            NumberOfFailedTries = 3;
        }

        public Belz RegisterWorker<TWorker>(string name)
            where TWorker : BeltzWorker, new()
        {
            _workers[name] = typeof (TWorker);
            return this;
        }

        public int NumberOfFailedTries { get; set; }
        public int QueueCheckFrequency { get; set; }
        public int OperationalThreadsCount { get; set; }

        private Thread _mainThread;
        private readonly AutoResetEvent _stop = new AutoResetEvent(false);

        public void Run()
        {
            if (_mainThread != null)
                return;
            _stop.Reset();
            _mainThread = new Thread(Runner);
            _mainThread.Start();
        }

        public void Stop()
        {
            if (_mainThread == null)
                return;
            _stop.Set();
            Thread.Sleep(1000);
            if (_mainThread.ThreadState == ThreadState.Running)
                _mainThread.Abort();
            _mainThread = null;
        }

        private static Int32 _inProcess;

        private void Runner(object o)
        {
            _inProcess = OperationalThreadsCount;
            while (true)
            {
                if (_inProcess <= 0)
                {
                    if (_stop.WaitOne(100))
                        return;
                }
                else
                {
                    var messages = _queueProvider.Retrieve(_inProcess).ToArray();
                    if (messages.Length == 0)
                    {
                        if (_stop.WaitOne(1000 * QueueCheckFrequency))
                            return;
                    }
                    else
                    {
                        foreach (var queueMessage in messages)
                        {
                            Type workerType;
                            if (!_workers.TryGetValue(queueMessage.Worker, out workerType))
                                continue;
                            WorkerExecutor.Execute(this, workerType, queueMessage, _queueProvider);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public void OnError(string message, Exception exception)
        {
            if (Error != null)
                Error(message, exception);
        }

        public void OnWorkerStarted(QueueMessage message)
        {
            Interlocked.Decrement(ref _inProcess);
        }

        public void OnWorkerEnded(QueueMessage message, bool success)
        {
            if (!success)
            {
                message.IncreaseTry();
                if (NumberOfFailedTries >= message.TryNo)
                    _queueProvider.Enqueue(message);
            }
            _queueProvider.Remove(message);
            Interlocked.Increment(ref _inProcess);
        }
    }
}
