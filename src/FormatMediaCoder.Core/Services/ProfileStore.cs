using System.Text.Json;
using System.Text.Json.Serialization;
using FormatMediaCoder.Core.Models;
using FormatMediaCoder.Core.Profiles;

namespace FormatMediaCoder.Core.Services;

/// <summary>
/// Carga y guarda perfiles como JSON, uno por archivo, en
/// <c>%AppData%\FormatMediaCoder\profiles\</c> (brief §4.1). Esto resuelve de
/// paso el pendiente de "presets guardables" y permite compartirlos entre equipos.
///
/// Los perfiles de fábrica no se persisten: se ofrecen siempre y el usuario puede
/// duplicarlos para editarlos. Un perfil de usuario con el mismo id que uno de
/// fábrica lo sobrescribe en la lista final.
///
/// La carpeta se inyecta por el constructor para poder testear sin tocar %AppData%.
/// </summary>
public sealed class ProfileStore
{
    private readonly string _dir;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public ProfileStore(string profilesDirectory)
    {
        _dir = profilesDirectory;
    }

    /// <summary>Ruta por defecto en producción: %AppData%\FormatMediaCoder\profiles\.</summary>
    public static string DefaultDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "FormatMediaCoder", "profiles");
    }

    /// <summary>Perfiles de fábrica + los del usuario, con el usuario ganando por id.</summary>
    public List<TargetProfile> LoadAll()
    {
        var byId = new Dictionary<string, TargetProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in BuiltinProfiles.All())
            byId[p.Id] = p;

        foreach (var user in LoadUserProfiles())
            byId[user.Id] = user; // el del usuario sobrescribe al de fábrica con igual id

        return byId.Values.OrderBy(p => p.System).ThenBy(p => p.Name).ToList();
    }

    public List<TargetProfile> LoadUserProfiles()
    {
        var result = new List<TargetProfile>();
        if (!Directory.Exists(_dir)) return result;

        foreach (var file in Directory.EnumerateFiles(_dir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var p = JsonSerializer.Deserialize<TargetProfile>(json, JsonOpts);
                if (p is not null && !string.IsNullOrEmpty(p.Id)) result.Add(p);
            }
            catch
            {
                // Un perfil corrupto no debe tumbar la app; se ignora en silencio.
            }
        }
        return result;
    }

    /// <summary>Guarda (o actualiza) un perfil de usuario. Los de fábrica no se guardan.</summary>
    public void Save(TargetProfile profile)
    {
        var errors = Validate(profile);
        if (errors.Count > 0)
            throw new ArgumentException("Perfil inválido: " + string.Join(" ", errors));

        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, profile.Id + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(profile, JsonOpts));
    }

    public void Delete(string id)
    {
        var path = Path.Combine(_dir, id + ".json");
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>Validación mínima antes de guardar.</summary>
    public static List<string> Validate(TargetProfile p)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(p.Id) || !System.Text.RegularExpressions.Regex.IsMatch(p.Id, "^[a-z0-9][a-z0-9-]*$"))
            errors.Add("id inválido (usa minúsculas, números y guiones).");
        if (string.IsNullOrWhiteSpace(p.Name))
            errors.Add("falta el nombre.");
        if (p.Complete)
        {
            if (string.IsNullOrEmpty(p.Video.Codec)) errors.Add("un perfil completo necesita video.codec.");
            if (string.IsNullOrEmpty(p.Video.Container)) errors.Add("un perfil completo necesita video.container.");
        }
        return errors;
    }
}
