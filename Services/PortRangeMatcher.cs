using System;

namespace PortManager.Services;

public static class PortRangeMatcher
{
    public static bool Matches(string value, int port)
    {
        foreach (var part in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part, out var single) && single == port)
                return true;

            var bounds = part.Split('-', 2, StringSplitOptions.TrimEntries);
            if (bounds.Length == 2 &&
                int.TryParse(bounds[0], out var start) &&
                int.TryParse(bounds[1], out var end) &&
                port >= start && port <= end)
                return true;
        }

        return false;
    }
}
