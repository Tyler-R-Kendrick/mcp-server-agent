namespace SecretAgent;

public record SecretAgentOptions(
    string Key,
    Uri? Endpoint = null,
    string? NewLine = null,
    string? ModelId = "gpt-4o")
{
    public string NewLine { get; init; } = NewLine ?? Environment.NewLine;
    public Uri Endpoint { get; init; } = Endpoint ?? new("https://models.inference.ai.azure.com");
}
