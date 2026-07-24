using FormatMediaCoder.Core.Models;
using FormatMediaCoder.Core.Services;

namespace FormatMediaCoder.App.Services;

/// <summary>
/// Acceso a los perfiles de destino desde la app: envuelve el <see cref="ProfileStore"/>
/// del Core apuntando a %AppData%\FormatMediaCoder\profiles\ (brief §4.1).
/// </summary>
public sealed class ProfileService
{
    private readonly ProfileStore _store;

    public ProfileService()
    {
        _store = new ProfileStore(ProfileStore.DefaultDirectory());
    }

    public List<TargetProfile> LoadAll() => _store.LoadAll();

    public void Save(TargetProfile profile) => _store.Save(profile);

    public void Delete(string id) => _store.Delete(id);
}
