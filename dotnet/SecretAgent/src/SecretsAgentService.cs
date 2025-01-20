namespace SecretAgent;

public class SecretsAgentService<TSecret>(
    SecretsAgent<TSecret> secretsAgent)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => secretsAgent.WriteSecrets(stoppingToken);
}
