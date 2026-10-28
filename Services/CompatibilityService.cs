using System.Text.Json;
using SPTarkov.DI.Annotations;

namespace PPES.Services;

public sealed record CompatNote(bool Warning, string En, string Pt);

/// <summary>Reads other mods' files (never writes) to explain overlaps on the page.</summary>
[Injectable(InjectionType.Singleton)]
public sealed class CompatibilityService(ConfigService config)
{
    private List<CompatNote>? _notes;

    public IReadOnlyList<CompatNote> Notes => _notes ??= Build();

    public void Refresh() => _notes = null;

    private List<CompatNote> Build()
    {
        var notes = new List<CompatNote>();
        var modsDir = Directory.GetParent(config.ModPath)?.FullName;
        if (modsDir is null || !Directory.Exists(modsDir)) return notes;
        var dirs = Directory.GetDirectories(modsDir);

        string? Find(string dll) => dirs.FirstOrDefault(d => File.Exists(Path.Combine(d, dll)));

        if (Find("acidphantasm-botplacementsystem.dll") is not null)
            notes.Add(new(false,
                "ABPS rebuilds boss spawns after every raid. PPES applies its boss events on top of that, at the start of each raid, so both work together.",
                "O ABPS refaz os spawns de boss depois de cada raid. O PPES aplica os eventos de boss por cima disso, no início de cada raid, então os dois funcionam juntos."));

        if (Find("ServerValueModifier.dll") is { } svm)
            notes.AddRange(ReadSvm(svm));

        return notes;
    }

    private static IEnumerable<CompatNote> ReadSvm(string svmDir)
    {
        JsonElement events;
        try
        {
            using var loader = JsonDocument.Parse(File.ReadAllText(Path.Combine(svmDir, "Loader", "loader.json")));
            var preset = loader.RootElement.GetProperty("CurrentlySelectedPreset").GetString();
            var raw = File.ReadAllText(Path.Combine(svmDir, "Presets", preset + ".json")).TrimStart('﻿');
            using var doc = JsonDocument.Parse(raw);
            var raids = doc.RootElement.GetProperty("Raids");
            if (!raids.GetProperty("EnableRaids").GetBoolean()) yield break;
            events = raids.GetProperty("RaidEvents").Clone();
        }
        catch
        {
            yield break;
        }

        bool On(string key) => events.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.True;

        if (On("Christmas") || On("Halloween"))
            yield return new(true,
                "SVM is forcing Christmas or Halloween (Raids section). That runs after PPES and turns the event on even if it is off here. Turn it off in SVM to let PPES decide.",
                "O SVM está forçando Natal ou Halloween (seção Raids). Isso roda depois do PPES e liga o evento mesmo que ele esteja desligado aqui. Desligue no SVM para o PPES decidir.");
        if (On("NonSeasonalQuests"))
            yield return new(false,
                "SVM shows every non-seasonal event quest, so quests here may already be visible with their event off.",
                "O SVM mostra todas as quests de evento não sazonais, então as quests daqui podem já aparecer com o evento desligado.");
        if (On("BossesOnCustoms") || On("BossesOnReserve") || On("GlukharLabs") || On("GoonsFactory") || On("KillaFactory") || On("TagillaInterchange") || On("CultistBosses"))
            yield return new(false,
                "SVM has boss events on too. They stack with the PPES boss events, so a boss may show up twice.",
                "O SVM também tem eventos de boss ligados. Eles somam com os do PPES, então um boss pode aparecer duas vezes.");
    }
}
