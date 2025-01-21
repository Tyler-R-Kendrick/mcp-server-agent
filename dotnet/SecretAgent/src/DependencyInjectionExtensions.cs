using FluentValidation;
using Microsoft.Extensions.AI;
using SecretAgent;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSecretAgent<TSecret>(
        this IServiceCollection services,
        Func<IServiceProvider, IValidator<TSecret>>? validatorFactory = null,
        Func<IServiceProvider, IChatClient>? chatClientFactory = null)
        where TSecret : class
    {
        validatorFactory ??= provider => provider.GetRequiredService<IValidator<TSecret>>();
        chatClientFactory ??= provider => provider.GetRequiredService<IChatClient>();
                services.AddHostedService<SecretsAgentService<TSecret>>();
        services.AddSingleton<SecretsAgent<TSecret>>(provider => new(
            chatClient: provider.GetRequiredService<IChatClient>(),
            history: [],
            conversation: provider.GetRequiredService<SecretsConversation<TSecret>>()));
        services.AddTransient<TSecret>();
        services.AddSingleton<SecretsConversation<TSecret>>();
        services.AddSingleton<SecretConversation>(provider => new(
            stdinFactory: () => Console.OpenStandardInput(),
            stdoutFactory: () => Console.OpenStandardOutput(),
            validator: provider.GetRequiredService<IValidator<TSecret>>(),
            options: provider.GetRequiredService<SecretAgentOptions>()
        ));
        services.AddTransient(validatorFactory);
        services.AddChatClient(chatClientFactory);
        return services;
    }
}