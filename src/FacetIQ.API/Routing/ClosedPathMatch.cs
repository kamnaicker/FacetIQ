namespace FacetIQ.API.Routing;

// The router ignores a trailing slash, so code that matches a raw request path against a route must
// too. Shared so every such comparison agrees.
public static class ClosedPathMatch
{
    // Keeps the root "/" intact rather than trimming it to an empty string.
    public static string Normalize(string path)
    {
        if (path.Length <= 1 || path[^1] != '/')
        {
            return path;
        }

        var trimmed = path.TrimEnd('/');

        return trimmed.Length == 0 ? "/" : trimmed;
    }
}
