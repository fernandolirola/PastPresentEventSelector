using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Mono.Cecil;

namespace PPES.Prepatch;

/// <summary>Adds PPES's five faction roles to the client enum before the game assembly loads.</summary>
public static class FactionTypesPatch
{
    private static readonly (int Id, string Name)[] Types =
    {
        (731001, "ppesCleanupCrew"),
        (731002, "ppesSmuggler"),
        (731003, "ppesPillager"),
        (731004, "ppesGus"),
        (731005, "ppesBasmach"),
    };

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void Patch(ref AssemblyDefinition assembly)
    {
        var type = assembly.MainModule.GetType("EFT.WildSpawnType")
            ?? throw new InvalidOperationException("EFT.WildSpawnType was not found");
        foreach (var custom in Types)
        {
            if (HasNameOrValue(type, custom.Name, custom.Id))
            {
                Logger.CreateLogSource("PPES Prepatch").LogWarning($"Skipped duplicate bot role {custom.Name} ({custom.Id})");
                continue;
            }
            type.Fields.Add(new FieldDefinition(custom.Name,
                FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, type)
            {
                Constant = custom.Id,
            });
        }
    }

    private static bool HasNameOrValue(TypeDefinition type, string name, int value)
    {
        foreach (var field in type.Fields)
        {
            if (field.Name == name) return true;
            if (field.HasConstant && field.Constant is int current && current == value) return true;
        }
        return false;
    }
}
