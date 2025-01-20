using FluentValidation;
using SecretAgent;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<SecretsAgentService<Secrets>>();
        services.AddSingleton<SecretsAgent<Secrets>>(provider => new(
            stdinFactory: () => Console.OpenStandardInput(),
            stdoutFactory: () => Console.OpenStandardOutput(),
            initialData: new(null, null, null),
            validator: provider.GetRequiredService<IValidator<Secrets>>(),
            logger: provider.GetRequiredService<ILogger<SecretsAgent<Secrets>>>()));
        services.AddTransient<IValidator<Secrets>, SecretsValidator>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        //logging.AddConsole();
    });

var host = builder.Build();
await host.RunAsync();

record Secrets(string? AccountKey, string? AccountName, string? ConnectionString);

internal class SecretsValidator : AbstractValidator<Secrets>
{
    public SecretsValidator()
    {
        RuleFor(x => x.AccountKey).NotNull().NotEmpty();
        RuleFor(x => x.AccountName).NotNull().NotEmpty();
        RuleFor(x => x.ConnectionString).NotNull().NotEmpty();
    }
}
