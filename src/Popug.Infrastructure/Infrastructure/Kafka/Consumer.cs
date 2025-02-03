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
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Popug.Infrastructure.Kafka
{
    /// <summary>
    ///     A simple example demonstrating how to set up a Kafka consumer as an
    ///     IHostedService.
    /// </summary>
    public class Consumer : BackgroundService
    {
        private readonly string _topic;
        private readonly IConsumer<string, long> _consumer;

        public Consumer(IConfiguration config)
        {
            var consumerConfig = new ConsumerConfig();
            consumerConfig.EnableAutoCommit = false;
            consumerConfig.AutoOffsetReset = AutoOffsetReset.Latest;
            consumerConfig.EnablePartitionEof = true;
            consumerConfig.GroupId = config.GetSection("Kafka:ConsumerSettings")["ConsumerGroup"];
            consumerConfig.BootstrapServers = config.GetSection("Kafka:ConsumerSettings")["BootstrapServers"];
            
            _topic = "auth-cud";
            _consumer = new ConsumerBuilder<string, long>(consumerConfig).Build();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
        }
        
        private void StartConsumerLoop(CancellationToken cancellationToken)
        {
            _consumer.Subscribe(_topic);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var cr = _consumer.Consume(cancellationToken);

                    // Handle message...
                    Console.WriteLine($"{cr.Message.Key}: {cr.Message.Value}ms");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException e)
                {
                    // Consumer errors should generally be ignored (or logged) unless fatal.
                    Console.WriteLine($"Consume error: {e.Error.Reason}");

                    if (e.Error.IsFatal)
                    {
                        // https://github.com/edenhill/librdkafka/blob/master/INTRODUCTION.md#fatal-consumer-errors
                        break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unexpected error: {e}");
                    break;
                }
            }
        }
        
        public override void Dispose()
        {
            _consumer.Close(); // Commit offsets and leave the group cleanly.
            _consumer.Dispose();

            base.Dispose();
        }
    }
}
