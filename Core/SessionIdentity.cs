using System.Collections.ObjectModel;

namespace MultiBox.Core;

public static class SessionIdentity
{
    public static IReadOnlyDictionary<string, string> Colors { get; } = new ReadOnlyDictionary<string, string>(
        new Dictionary<string, string> {
            ["lime"] = "#C6FA46", ["blue"] = "#7CB7FF", ["violet"] = "#BCA1FF",
            ["rose"] = "#F5A1BB", ["amber"] = "#EFC478", ["teal"] = "#79D9CB"
        });
}
