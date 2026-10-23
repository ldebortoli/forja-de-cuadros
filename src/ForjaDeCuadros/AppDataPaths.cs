using System;
using System.IO;

namespace ForjaDeCuadros;

public static class AppDataPaths
{
    public static string DataDirectory => Environment.GetEnvironmentVariable("FORJA_DATA_DIRECTORY")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ForjaDeCuadros");
}
