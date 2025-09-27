namespace DAXRay.Core.Utils;

public static class NameNormalizer
{
    public static string Normalize(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;

        // Split into table + measure parts
        var parts = fullName.Split('.', 2);
        if (parts.Length != 2)
            return NormalizeToken(fullName);

        var table = NormalizeToken(parts[0]);
        var measure = NormalizeToken(parts[1]);

        return $"{table}.{measure}";
    }

    private static string NormalizeToken(string token)
    {
        return new string(
            token
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit) // keep only a–z, 0–9
                .ToArray()
        );
    }
}
