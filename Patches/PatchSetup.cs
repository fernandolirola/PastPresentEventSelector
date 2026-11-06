using PPES.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace PPES.Patches;

/// <summary>Turns the Harmony patches on once the load-time events are applied.</summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 5000001)]
public sealed class PatchSetup(RaidBuilder raids, TraderMessageService traderMessages) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        RaidLocationPatch.Builder = raids;
        new RaidLocationPatch().Enable();
        new EndRaidRolePatch().Enable();
        GameStartPatch.Service = traderMessages;
        new GameStartPatch().Enable();
        return Task.CompletedTask;
    }
}
