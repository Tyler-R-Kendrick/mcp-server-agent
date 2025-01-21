using System.Reactive.Linq;
using Microsoft.Extensions.AI;

namespace SecretAgent;

public class SecretsAgent<TSecret>(
    IChatClient chatClient,
    ChatMessage[] history,
    SecretsConversation<TSecret> conversation)
{
    private IObservable<TSecret> WriteSecretsObservable(
        CancellationToken stoppingToken)
        => Observable.Create<TSecret>(async (observer, cancellationToken) =>
        {
            await foreach (var secret in conversation
                .WriteSecrets(cancellationToken)
                .WithCancellation(stoppingToken))
            {
                observer.OnNext(secret);
            }
            observer.OnCompleted();
        });

    public async Task<TSecret> WriteSecrets(
        CancellationToken stoppingToken)
    {
        var secrets = WriteSecretsObservable(stoppingToken);
        using var observer = secrets.Subscribe(
            //TODO: find a way to write secrets to an injected config
            //onNext: async secret => await config.WriteAsync(secret, stoppingToken)
        );
        return await secrets.LastAsync();
    }

    public async Task<string> ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var chat = chatClient.AsBuilder()
            .UseFunctionInvocation()
            .Build();
        ChatMessage systemMessage = new(ChatRole.System, @"
            You are an AI agent that is resoponsible for collecting secrets.
            If there are errors in the most recent tooling responses,
            you will run a process to collect those secrets. 
        ");
        ChatMessage[] localHistory = [ systemMessage, ..history ];
        var result = await chat.CompleteAsync(history, new()
        {
            ToolMode = ChatToolMode.Auto,
            Tools = [ AIFunctionFactory.Create(WriteSecrets) ]
        }, stoppingToken);
        return result.ToString();
    }
}
