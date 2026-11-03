using System.Reflection;
using System.Text.Json.Serialization;

namespace AgentFrameworkProto1;

public sealed record PolicyEntry(
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url);

public sealed record PolicyGroup(
    [property: JsonPropertyName("heading")] string Heading,
    [property: JsonPropertyName("policies")] List<PolicyEntry> Policies);

/// <summary>
/// Hardcoded index of Kutztown University's policy register, scraped once
/// from https://www.kutztown.edu/about-ku/administration/governance/policy-register.html
/// and embedded as a JSON resource. Never re-fetched at runtime.
/// </summary>
public static class PolicyIndex
{
    public static readonly List<PolicyGroup> Groups = Load();

    public static readonly Dictionary<string, PolicyEntry> ByNumber =
        Groups
            .SelectMany(g => g.Policies)
            .ToDictionary(p => p.Number, StringComparer.OrdinalIgnoreCase);

    private static List<PolicyGroup> Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("policy-index.json", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        return System.Text.Json.JsonSerializer.Deserialize<List<PolicyGroup>>(stream)
            ?? throw new InvalidOperationException("policy-index.json deserialized to null.");
    }
}
