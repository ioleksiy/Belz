using System.Collections.Generic;
using System.Json;
using System.Linq;

namespace Belz.QueueProvider
{
    public sealed class QueueMessage
    {
        public string ID { get; private set; }
        public string Worker { get; private set; }
        public int Timeout { get; private set; }
        public int TryNo { get; private set; }
        public IDictionary<string, string> Parameters { get { return _parameters; } }

        private readonly IDictionary<string, string> _parameters = new Dictionary<string, string>();

        public QueueMessage(string worker, int timeout = 0)
        {
            Worker = worker;
            Timeout = timeout;
            TryNo = 1;
        }

        public QueueMessage WithParameter(string key, string value)
        {
            _parameters[key] = value;
            return this;
        }

        public QueueMessage IncreaseTry()
        {
            TryNo++;
            return this;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Worker, ID ?? "none");
        }

        public static QueueMessage FromJsonString(string id, string data)
        {
            var json = JsonValue.Parse(data);
            var m = new QueueMessage(id)
                        {
                            Worker = json["w"].ReadAs<string>(),
                            Timeout = json["t"].ReadAs<int>(),
                            TryNo = json["r"].ReadAs<int>(),
                            ID = id
                        };
            foreach (var item in json["p"])
            {
                m.WithParameter(item.Key, item.Value.ReadAs<string>());
            }
            return m;
        }

        public JsonValue ToJson()
        {
            return new JsonObject(
                new KeyValuePair<string, JsonValue>("w", Worker),
                new KeyValuePair<string, JsonValue>("p",
                                                    new JsonObject(
                                                        Parameters.Select(
                                                            x =>
                                                            new KeyValuePair<string, JsonValue>(x.Key,
                                                                                                new JsonPrimitive(
                                                                                                    x.Value))))),
                new KeyValuePair<string, JsonValue>("t", Timeout),
                new KeyValuePair<string, JsonValue>("r", TryNo)
                );
        }
    }
}
