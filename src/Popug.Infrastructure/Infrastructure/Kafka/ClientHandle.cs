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

using System;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;


namespace Popug.Infrastructure.Kafka
{
    /// <summary>
    ///     Wraps a Confluent.Kafka.IProducer instance, and allows for basic
    ///     configuration of this via IConfiguration.
    ///    
    ///     ClientHandle does not provide any way for messages to be produced
    ///     directly. Instead, it is a dependency of KafkaDependentProducer. You
    ///     can create more than one instance of KafkaDependentProducer (with
    ///     possibly differing K and V generic types) that leverages the same
    ///     underlying producer instance exposed by the Handle property of this
    ///     class. This is more efficient than creating separate
    ///     Confluent.Kafka.IProducer instances for each Message type you wish to
    ///     produce.
    /// </summary>
    public class ClientHandle : IDisposable
    {
        private readonly IProducer<byte[], byte[]> _producer;

        public ClientHandle(IConfiguration config)
        {
            var conf = new ProducerConfig();
            conf.BootstrapServers = config.GetSection("Kafka:ProducerSettings")["BootstrapServers"];
            
            _producer = new ProducerBuilder<byte[], byte[]>(conf).Build();
        }

        public Handle Handle { get => _producer.Handle; }

        public void Dispose()
        {
            // Block until all outstanding produce requests have completed (with or
            // without error).
            _producer.Flush();
            _producer.Dispose();
        }
    }
}
