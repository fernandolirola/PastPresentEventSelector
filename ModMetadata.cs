using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace PPES;

public record PpesMetadata : IModMetadata, IModBlazorMetadata
{
    public string ModGuid { get; init; } = "com.zezinho.pastpresenteventselector";
    public string Name { get; init; } = "PastPresentEventSelector";
    public string Author { get; init; } = "zezinho";
    public List<string>? Contributors { get; init; } = [];
    public Version Version { get; init; } = new(typeof(PpesMetadata).Assembly.GetName().Version!.ToString(3));
    public Range SptVersion { get; init; } = new("~4.1.6");
    public List<string>? Incompatibilities { get; init; } = [];
    public Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        ["com.morebotsapi.tacticaltoaster"] = new(">=2.1.1"),
    };
    public string? Url { get; init; } = "https://github.com/fernandolirola/PastPresentEventSelector";
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;

    public string? WWWRootUrl { get; init; }
    public string? HomePage { get; init; } = "/ppes";
    public string? HomePageDescription { get; init; } = "Pick which seasonal and past events are active.";
}
