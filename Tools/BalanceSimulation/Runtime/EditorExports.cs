// Large payloads go straight to disk; the CLI receives only an acknowledgement.
object WriteEditorExport(object payload, string requestedPath) {
    string path = System.IO.Path.GetFullPath(requestedPath);
    string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Reports/BalanceSimulation")) + System.IO.Path.DirectorySeparatorChar;
    if (!path.StartsWith(root, System.StringComparison.OrdinalIgnoreCase))
        throw new System.InvalidOperationException("Exports must stay under Reports/BalanceSimulation.");
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
    // Localization also ships JsonConvert; resolve the actual Newtonsoft assembly.
    var jsonType = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert", true);
    string json = (string)jsonType.GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { payload });
    string temporary = path + "." + System.Guid.NewGuid().ToString("N") + ".tmp";
    try {
        System.IO.File.WriteAllText(temporary, json);
        if (System.IO.File.Exists(path)) System.IO.File.Replace(temporary, path, null);
        else System.IO.File.Move(temporary, path);
    } finally {
        if (System.IO.File.Exists(temporary)) System.IO.File.Delete(temporary);
    }
    return new { exported = true, exportPath = path };
}
