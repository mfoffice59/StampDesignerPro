using System.IO;
using System.Text.Json;
using StampDesignerPro.Models;
namespace StampDesignerPro.Services;
public static class ProjectService
{
    public static void Save(string path, StampProject project){ var json=JsonSerializer.Serialize(project,new JsonSerializerOptions{WriteIndented=true}); File.WriteAllText(path,json); }
    public static StampProject Load(string path){ var json=File.ReadAllText(path); return JsonSerializer.Deserialize<StampProject>(json)??new StampProject(); }
}
