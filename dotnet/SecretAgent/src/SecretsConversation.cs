using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FluentValidation;

namespace SecretAgent;

public class SecretsConversation<TSecret>(
    SecretConversation secretConversation,
    TSecret initialData,
    IValidator<TSecret?> validator,
    ILogger<SecretsAgent<TSecret>> logger)
{
    private readonly Type templateType = typeof(TSecret);
    private PropertyInfo GetProperty(string propertyName)
        => templateType.GetProperty(propertyName)
            ?? throw new KeyNotFoundException($"Property {propertyName} not found.");
    private string? GetPropertyString(string propertyName)
        => GetProperty(propertyName).GetValue(initialData)?.ToString();

    private TSecret SetProperty(
        string propertyName,
        string? secretValue,
        TSecret secrets,
        ILogger logger)
    {
        var localSecrets = secrets;
        var property = GetProperty(propertyName);
        property?.SetValue(localSecrets, secretValue);
        logger.LogInformation("Property {0} set to {1}", propertyName, secretValue);
        return localSecrets;
    }

    public async IAsyncEnumerable<TSecret> WriteSecrets(
        [EnumeratorCancellation] CancellationToken stoppingToken)
    {
        var secrets = initialData;
        yield return secrets;
        var validationResult = validator.Validate(secrets);
        while (!stoppingToken.IsCancellationRequested && !validationResult.IsValid)
        {
            var properties = validationResult.ToDictionary().Keys.Distinct();
            foreach (var propertyName in properties)
            {
                var propertyValue = GetPropertyString(propertyName);
                var conversationResult = secretConversation.Converse(
                    secrets, propertyName, propertyValue, stoppingToken);
                var secretValue = await conversationResult.LastAsync(stoppingToken);
                secrets = SetProperty(propertyName, secretValue, secrets, logger);
            }
            yield return secrets;
            validationResult = validator.Validate(secrets);
            logger.LogInformation("Validating secrets {0}", validationResult.IsValid);
        }
        logger.LogInformation("Secrets retrieved and validated successfully.");
    }
}
