using KlioApp.Core.Models;

namespace KlioApp.Core.Interfaces;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
