using System.ComponentModel;
using HtmlAgilityPack;

namespace AgentFrameworkProto1;

public static class PolicyTools
{
    private static readonly HttpClient HttpClient = new();

    [Description("Lists every Kutztown University policy (policy number and title), grouped under its category heading.")]
    public static string ReadIndex()
    {
        var sections = PolicyIndex.Groups.Select(group =>
        {
            var lines = group.Policies.Select(p => $"  {p.Number} - {p.Title}");
            return $"{group.Heading}\n{string.Join('\n', lines)}";
        });

        return string.Join("\n\n", sections);
    }

    [Description("Reads the full text of a single Kutztown University policy by its policy number (e.g. 'ACA-001'). Returns a 404-style error if the policy number doesn't exist.")]
    public static async Task<string> ReadPolicy(
        [Description("The policy number, e.g. 'ACA-001' or 'HR-002'.")] string policyNumber)
    {
        if (!PolicyIndex.ByNumber.TryGetValue(policyNumber, out var policy))
        {
            return $"404 Not Found: no policy with number '{policyNumber}'.";
        }

        var html = await HttpClient.GetStringAsync(policy.Url);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var main = doc.DocumentNode.SelectSingleNode("//main");
        var text = main is not null
            ? HtmlEntity.DeEntitize(main.InnerText)
            : HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);

        var lines = text
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);

        return $"{policy.Number} - {policy.Title}\n\n{string.Join('\n', lines)}";
    }
}
