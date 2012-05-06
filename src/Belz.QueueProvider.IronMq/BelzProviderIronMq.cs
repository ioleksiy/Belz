using System;
using System.Collections.Generic;
using System.Linq;
using Rest4Net.IronMq;

namespace Belz.QueueProvider.IronMq
{
    public class BelzProviderIronMq : IQueueProvider
    {
        private readonly IronMqProvider _provider;
        private readonly string _queueName;

        public BelzProviderIronMq(string ironMqToken, string ironMqProjectId, string ironMqQueueName, bool useAws = true)
        {
            NumberOfRequestsMade = 0;
            _provider = new IronMqProvider(ironMqToken, ironMqProjectId, useAws ? Provider.AWS : Provider.Rackspace);
            _queueName = ironMqQueueName;
        }

        public IEnumerable<QueueMessage> Retrieve(int messagesCount = 1)
        {
            NumberOfRequestsMade++;
            return _provider.GetMessages(_queueName, messagesCount).Select(x => QueueMessage.FromJsonString(x.ID, x.Body));
        }

        public bool Remove(QueueMessage message)
        {
            NumberOfRequestsMade++;
            return _provider.RemoveMessage(_queueName, message.ID);
        }

        private static Message ToMessage(QueueMessage message)
        {
            var m = new Message(message.ToJson().ToString());
            if (message.Timeout > 0)
            {
                var to = message.Timeout / 1000;
                m.Timeout += to;
                m.ExpiresIn += to;
            }
            return m;
        }

        public void Enqueue(params QueueMessage[] messages)
        {
            if (messages.Length == 0)
                throw new ArgumentException("No messages passed", "messages");
            _provider.AddMessages(_queueName, messages.Select(ToMessage).ToArray());
            NumberOfRequestsMade++;
        }

        public int AvailableMessages
        {
            get
            {
                NumberOfRequestsMade++;
                return _provider.Queue(_queueName).Size;
            }
        }

        public int NumberOfRequestsMade { get; private set; }

        public void Dispose()
        {
            _provider.Dispose();
        }
    }
}
