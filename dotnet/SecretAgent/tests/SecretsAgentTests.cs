using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;

namespace SecretAgent.Tests
{
    public class SecretsAgentTests
    {
        private SecretsAgent _secretsAgent;
        private List<Secret> _secrets;
        private Pipe _pipeline;
        private CancellationTokenSource _cancellationTokenSource;

        [SetUp]
        public void Setup()
        {
            var logger = new LoggerFactory().CreateLogger<SecretsAgent>();
            var resiliencyPolicy = Policy.NoOpAsync();
            _secretsAgent = new SecretsAgent(logger, 2, resiliencyPolicy);
            _secrets = new List<Secret>();
            _pipeline = new Pipe();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        [Test]
        public async Task TestCorrectInput()
        {
            var writeTask = WriteToPipelineAsync(_pipeline.Writer, new List<string> { "key1:value1", "key2:value2" });
            var readTask = _secretsAgent.ReadSecretsAsync(_pipeline.Reader, _secrets, _cancellationTokenSource.Token);

            await Task.WhenAll(writeTask, readTask);

            Assert.AreEqual(2, _secrets.Count);
            Assert.AreEqual("key1", _secrets[0].Key);
            Assert.AreEqual("value1", _secrets[0].Value);
            Assert.AreEqual("key2", _secrets[1].Key);
            Assert.AreEqual("value2", _secrets[1].Value);
        }

        [Test]
        public async Task TestIncorrectInput()
        {
            var writeTask = WriteToPipelineAsync(_pipeline.Writer, new List<string> { "key1:value1", "invalid_input" });
            var readTask = _secretsAgent.ReadSecretsAsync(_pipeline.Reader, _secrets, _cancellationTokenSource.Token);

            await Task.WhenAll(writeTask, readTask);

            Assert.AreEqual(1, _secrets.Count);
            Assert.AreEqual("key1", _secrets[0].Key);
            Assert.AreEqual("value1", _secrets[0].Value);
        }

        [Test]
        public async Task TestOutputStream()
        {
            var writeTask = WriteToPipelineAsync(_pipeline.Writer, new List<string> { "key1:value1", "key2:value2", "key3:value3" });
            var readTask = _secretsAgent.ReadSecretsAsync(_pipeline.Reader, _secrets, _cancellationTokenSource.Token);

            await Task.WhenAll(writeTask, readTask);

            Assert.AreEqual(3, _secrets.Count);
            Assert.AreEqual("key1", _secrets[0].Key);
            Assert.AreEqual("value1", _secrets[0].Value);
            Assert.AreEqual("key2", _secrets[1].Key);
            Assert.AreEqual("value2", _secrets[1].Value);
            Assert.AreEqual("key3", _secrets[2].Key);
            Assert.AreEqual("value3", _secrets[2].Value);
        }

        private async Task WriteToPipelineAsync(PipeWriter writer, List<string> inputs)
        {
            foreach (var input in inputs)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                await writer.WriteAsync(bytes);
            }

            writer.Complete();
        }
    }
}
