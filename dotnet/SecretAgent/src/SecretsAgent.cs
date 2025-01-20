using System.Buffers;
using System.IO.Pipelines;
using System.Reactive.Linq;
using System.Text;
using FluentValidation;
using FluentValidation.Results;

namespace SecretAgent;

public class SecretsAgent<TSecret>(
    Func<Stream> stdinFactory,
    Func<Stream> stdoutFactory,
    TSecret initialData,
    IValidator<TSecret> validator,
    ILogger<SecretsAgent<TSecret>> logger)
{
    private async Task WriteError(
        string errorMessage,
        CancellationToken stoppingToken)
    {
        var errorBytes = Encoding.UTF8.GetBytes(errorMessage);
        var pipeWriter = PipeWriter.Create(stdoutFactory());
        await pipeWriter.WriteAsync(new(errorBytes), stoppingToken);
        await pipeWriter.CompleteAsync();
    }

    private async Task<string> ReadError(
        CancellationToken stoppingToken)
    {        
        var pipeReader = PipeReader.Create(stdinFactory());
        var readResult = await pipeReader.ReadAsync(stoppingToken);
        var buffer = readResult.Buffer.ToArray();
        return Encoding.UTF8.GetString(buffer);
    }

    private static TSecret SetProperty(
        string propertyName,
        string secretValue,
        TSecret secrets,
        ILogger logger)
    {
        var localSecrets = secrets;
        var property = typeof(TSecret).GetProperty(propertyName);
        property?.SetValue(localSecrets, secretValue);
        logger.LogInformation("Property {0} set to {1}", propertyName, secretValue);
        return localSecrets;
    }

    private async Task<TSecret> HandleError(
        string propertyName,
        IDictionary<string, string[]> errorDictionary,
        TSecret secrets,
        CancellationToken stoppingToken)
    {
        var errors = errorDictionary[propertyName].Distinct();
        var message = string.Join(",", errors);
        var errorMessage = $"{propertyName}: {message}";
        await WriteError(errorMessage, stoppingToken);
        var secretValue = await ReadError(stoppingToken);
        return SetProperty(propertyName, secretValue, secrets, logger);
    }

    private async Task<TSecret> HandleValidationResult(
        ValidationResult validationResult,
        TSecret secrets,
        CancellationToken stoppingToken)
    {
        var errorDictionary = validationResult.ToDictionary();
        foreach (var propertyName in errorDictionary.Keys)
        {
            secrets = await HandleError(
                propertyName,
                errorDictionary,
                secrets,
                stoppingToken);
        }
        return secrets;
    }

    public async Task WriteSecrets(
        CancellationToken stoppingToken)
    {
        var secrets = initialData;
        var validationResult = validator.Validate(secrets);
        while (!stoppingToken.IsCancellationRequested && !validationResult.IsValid)
        {
            secrets = await HandleValidationResult(
                validationResult,
                secrets,
                stoppingToken);
            validationResult = validator.Validate(secrets);
            logger.LogInformation("Validating secrets {0}", validationResult.IsValid);
        }
        logger.LogInformation("Secrets retrieved and validated successfully.");
    }
}
