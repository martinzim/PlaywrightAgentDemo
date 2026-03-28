namespace AiBrowserTester.Planning;
internal static class JsonExtraction
{
    public static string Extract(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstBrace = trimmed.IndexOf('{');
            var lastBrace = trimmed.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace) return trimmed[firstBrace..(lastBrace + 1)];
        }
        return trimmed;
    }
}