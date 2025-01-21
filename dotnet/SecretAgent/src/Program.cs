using FluentValidation;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(configHost =>
    {
        configHost
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>(true, true)
            .AddCommandLine(args);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSecretAgent(_ => new SecretsValidator());
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
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
