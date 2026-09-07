namespace Fateforged.Infrastructure.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

/// <summary>Reads JSON resources through Godot so packaged PCK content works in exports.</summary>
public static class GodotJsonResource
{
    public static string ReadAllText(string resourcePath)
    {
        using var file = Godot.FileAccess.Open(resourcePath, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
            throw new InvalidDataException(
                $"JSON resource was not found at '{resourcePath}' (Godot error: {Godot.FileAccess.GetOpenError()})."
            );
        return file.GetAsText();
    }

    public static IReadOnlyList<string> ListJsonFiles(string resourceDirectory)
    {
        using var directory = DirAccess.Open(resourceDirectory);
        if (directory == null)
            throw new InvalidDataException(
                $"JSON resource directory was not found at '{resourceDirectory}'."
            );

        var root = resourceDirectory.TrimEnd('/');
        return directory
            .GetFiles()
            .Where(fileName => fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .Select(fileName => $"{root}/{fileName}")
            .ToArray();
    }
}
