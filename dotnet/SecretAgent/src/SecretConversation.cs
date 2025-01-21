using System.Buffers;
using System.IO.Pipelines;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using FluentValidation;
using FluentValidation.Results;

namespace SecretAgent;

public class SecretConversation(
    Func<Stream> stdinFactory,
    Func<Stream> stdoutFactory,
    IValidator validator,
    SecretAgentOptions options)
{
    public static async Task WritePipe(
        PipeWriter pipeWriter,
        string message,
        CancellationToken stoppingToken)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await pipeWriter.WriteAsync(new(messageBytes), stoppingToken);
    }
    
    public async Task WriteError(
        string propertyName,
        string errorMessage,
        CancellationToken stoppingToken)
    {
        var pipeWriter = PipeWriter.Create(stdoutFactory());
        await WritePipe(pipeWriter, errorMessage, stoppingToken);
        await WritePipe(pipeWriter, options.NewLine, stoppingToken);
        await WritePipe(pipeWriter, $"{propertyName}: ", stoppingToken);
        await pipeWriter.CompleteAsync();
    }

    public async Task<string> ReadInputStream(
        CancellationToken stoppingToken)
    {        
        var pipeReader = PipeReader.Create(stdinFactory());
        var readResult = await pipeReader.ReadAsync(stoppingToken);
        var buffer = readResult.Buffer.ToArray();
        return Encoding.UTF8.GetString(buffer);
    }

    public async Task<string?> HandleError(
        string propertyName,
        IDictionary<string, string[]> errorDictionary,
        CancellationToken stoppingToken)
    {
        if(!errorDictionary.TryGetValue(propertyName, out string[]? value)) return null;
        var errors = value.Distinct();
        var message = string.Join(",", errors);
        var errorMessage = $"{propertyName}: {message}";
        await WriteError(propertyName, errorMessage, stoppingToken);
        return await ReadInputStream(stoppingToken);
    }

    public async IAsyncEnumerable<string?> Converse<T>(
        T? secretsInstance,
        string propertyName,
        string? secretValue,
        [EnumeratorCancellation]
        CancellationToken stoppingToken)
    {
        Task<ValidationResult> ValidateAsync()
        {
            ValidationContext<T?> validationContext = new(secretsInstance);
            validationContext.PropertyChain.Add(propertyName);
            validationContext.RootContextData.Add(propertyName, secretValue);
            return validator.ValidateAsync(validationContext, stoppingToken);
        }
        yield return secretValue;
        var validationResult = await ValidateAsync();
        while (!stoppingToken.IsCancellationRequested && !validationResult.IsValid)
        {
            secretValue = await HandleError(
                propertyName,
                validationResult.ToDictionary(),
                stoppingToken);
            yield return secretValue;
            validationResult = await ValidateAsync();
        }
    }
}
