using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Mono.Cecil;

namespace PPES.Prepatch;

/// <summary>Adds PPES's five faction roles to the client enum before the game assembly loads.</summary>
public static class FactionTypesPatch
{
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void Patch(ref AssemblyDefinition assembly)
    {
        var type = assembly.MainModule.GetType("EFT.WildSpawnType")
            ?? throw new InvalidOperationException("EFT.WildSpawnType was not found");
        foreach (var custom in PpesIds.Factions)
        {
            if (HasNameOrValue(type, custom.Role, custom.Id))
            {
                Logger.CreateLogSource("PPES Prepatch").LogWarning($"Skipped duplicate bot role {custom.Role} ({custom.Id})");
                continue;
            }
            type.Fields.Add(new FieldDefinition(custom.Role,
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
