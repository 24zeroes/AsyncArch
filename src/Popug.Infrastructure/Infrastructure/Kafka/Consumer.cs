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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Popug.Infrastructure.Kafka
{
    /// <summary>
    ///     A simple example demonstrating how to set up a Kafka consumer as an
    ///     IHostedService.
    /// </summary>
    public class Consumer : BackgroundService
    {
        private readonly string _topic;
        private readonly IConsumer<Null, string> _consumer;
        private readonly ILogger<Consumer> _logger;

        public Consumer(IConfiguration config, ILogger<Consumer> logger)
        {
            var consumerConfig = new ConsumerConfig();
            consumerConfig.GroupId = config.GetSection("Kafka:ConsumerSettings")["GroupId"];
            consumerConfig.BootstrapServers = config.GetSection("Kafka:ConsumerSettings")["BootstrapServers"];
            
            _topic = "auth-cud";
            _logger = logger;
            _consumer = new ConsumerBuilder<Null, string>(consumerConfig)
                .SetErrorHandler(HandleError)
                .SetLogHandler(HandleLog)
                .Build();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
        }
        
        protected void HandleError(IConsumer<Null, string> consumer, Error error)
        {
            _logger.LogError(error.Reason);
        }

        protected void HandleLog(IConsumer<Null, string> consumer, LogMessage message)
        {
            _logger.LogInformation(message.Message);
        }
        
        private void StartConsumerLoop(CancellationToken cancellationToken)
        {
            _logger.LogError("test");
            try
            {
                _consumer.Subscribe(_topic);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An error occured while starting consumer. {e.Message}");
                throw;
            }
            

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var cr = _consumer.Consume(cancellationToken);

                    // Handle message...
                    _logger.LogInformation($"Consumed message: {cr.Message.Value}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException e)
                {
                    // Consumer errors should generally be ignored (or logged) unless fatal.
                    _logger.LogError(e, $"Error occured while consuming message: {e.Message} {Environment.NewLine} {e.Error.Reason}");

                    if (e.Error.IsFatal)
                    {
                        // https://github.com/edenhill/librdkafka/blob/master/INTRODUCTION.md#fatal-consumer-errors
                        break;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Unexpected error {e.Message} {Environment.NewLine} {e.StackTrace}");
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
