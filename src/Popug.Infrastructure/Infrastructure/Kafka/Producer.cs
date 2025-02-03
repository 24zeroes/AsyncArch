// Copyright 2020 Confluent Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Refer to LICENSE for more information.

using Confluent.Kafka;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Popug.Contracts;


namespace Popug.Infrastructure.Kafka
{
    /// <summary>
    ///     Leverages the injected ClientHandle instance to allow
    ///     Confluent.Kafka.Message{K,V}s to be produced to Kafka.
    /// </summary>
    public class Producer
    {
        private readonly IProducer<Null, string> _producer;

        public Producer(ClientHandle handle)
        {
            _producer = new DependentProducerBuilder<Null, string>(handle.Handle).Build();
        }

        /// <summary>
        ///     Asychronously produce a message and expose delivery information
        ///     via the returned Task. Use this method of producing if you would
        ///     like to await the result before flow of execution continues.
        /// <summary>
        public Task ProduceAsync(string topic, IKafkaMessage message)
        {
            var m = new Message<Null, string>() {Value = JsonSerializer.Serialize(message, message.GetType())};
            return _producer.ProduceAsync(topic, m);
        }
        

        public void Flush(TimeSpan timeout)
            => _producer.Flush(timeout);
    }
}
