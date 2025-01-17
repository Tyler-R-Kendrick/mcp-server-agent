using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Reactive.Linq;
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
            var secretsObservable = _secretsAgent.CreateSecretsObservable(_cancellationTokenSource.Token);
            var secretsList = new List<Secret>();

            var subscription = secretsObservable.Subscribe(secret =>
            {
                secretsList.Add(secret);
            });

            var writeTask = WriteToPipelineAsync(_pipeline.Writer, new List<string> { "key1:value1", "key2:value2" });
            await writeTask;

            _cancellationTokenSource.Cancel();
            subscription.Dispose();

            Assert.AreEqual(2, secretsList.Count);
            Assert.AreEqual("key1", secretsList[0].Key);
            Assert.AreEqual("value1", secretsList[0].Value);
            Assert.AreEqual("key2", secretsList[1].Key);
            Assert.AreEqual("value2", secretsList[1].Value);
        }

        [Test]
        public async Task TestIncorrectInput()
        {
            var secretsObservable = _secretsAgent.CreateSecretsObservable(_cancellationTokenSource.Token);
            var secretsList = new List<Secret>();

            var subscription = secretsObservable.Subscribe(secret =>
            {
                secretsList.Add(secret);
            });

            var writeTask = WriteToPipelineAsync(_pipeline.Writer, new List<string> { "key1:value1", "invalid_input" });
            await writeTask;

            _cancellationTokenSource.Cancel();
            subscription.Dispose();

            Assert.AreEqual(1, secretsList.Count);
            Assert.AreEqual("key1", secretsList[0].Key);
            Assert.AreEqual("value1", secretsList[0].Value);
        }

        [Test]
        public async Task TestOutputStream()
        {
            var secretsObservable = _secretsAgent.CreateSecretsObservable(_cancellationTokenSource.Token);
            var secretsList = new List<Secret>();

            var subscription = secretsObservable.Subscribe(secret =>
            {
                secretsList.Add(secret);
            });

            var writeTask = WriteToPipelineAsync(_pipeline.Writer, new List<string> { "key1:value1", "key2:value2", "key3:value3" });
            await writeTask;

            _cancellationTokenSource.Cancel();
            subscription.Dispose();

            Assert.AreEqual(3, secretsList.Count);
            Assert.AreEqual("key1", secretsList[0].Key);
            Assert.AreEqual("value1", secretsList[0].Value);
            Assert.AreEqual("key2", secretsList[1].Key);
            Assert.AreEqual("value2", secretsList[1].Value);
            Assert.AreEqual("key3", secretsList[2].Key);
            Assert.AreEqual("value3", secretsList[2].Value);
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
