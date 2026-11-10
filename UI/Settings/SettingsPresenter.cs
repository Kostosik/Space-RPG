
using GameProject.Settings;

namespace GameProject.UI.Settings;

public sealed class SettingsPresenter
{
    private readonly GameSettings _settings;
    private readonly Action<DisplaySettings>? _applyDisplay;

    private GameSettings _draft;

    private static readonly ResolutionOption[] SupportedResolutions =
    [
        new("1280 x 720", 1280, 720),
        new("1366 x 768", 1366, 768),
        new("1600 x 900", 1600, 900),
        new("1920 x 1080", 1920, 1080),
        new("2560 x 1440", 2560, 1440)
    ];

    public SettingsPresenter(
        GameSettings settings,
        Action<DisplaySettings>? applyDisplay = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
        _applyDisplay = applyDisplay;
        _draft = settings.CreateCopy();
    }

    public void BeginEdit()
    {
        _draft = _settings.CreateCopy();
    }

    public SettingsViewData GetViewData()
    {
        var resolutions = SupportedResolutions.ToList();

        var selectedIndex = resolutions.FindIndex(
            option =>
                option.Width == _draft.Display.Width &&
                option.Height == _draft.Display.Height);

        // Сохраняем возможность отображать нестандартное
        // разрешение, которое уже установлено.
        if (selectedIndex < 0)
        {
            resolutions.Add(new ResolutionOption(
                $"{_draft.Display.Width} x {_draft.Display.Height}",
                _draft.Display.Width,
                _draft.Display.Height));

            selectedIndex = resolutions.Count - 1;
        }

        return new SettingsViewData(
            resolutions,
            selectedIndex,
            _draft.Display.Fullscreen,
            _draft.Display.VSync,
            _draft.Audio.MasterVolume,
            _draft.Audio.MusicVolume,
            _draft.Audio.EffectsVolume,
            _draft.Gameplay.ShowPlanetTooltips);
    }

    public void SetResolution(int index)
    {
        var options = GetViewData().Resolutions;

        if (index < 0 || index >= options.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var option = options[index];

        _draft.Display.Width = option.Width;
        _draft.Display.Height = option.Height;
    }

    public void SetFullscreen(bool value) =>
        _draft.Display.Fullscreen = value;

    public void SetVSync(bool value) =>
        _draft.Display.VSync = value;

    public void SetMasterVolume(float value) =>
        _draft.Audio.MasterVolume = value;

    public void SetMusicVolume(float value) =>
        _draft.Audio.MusicVolume = value;

    public void SetEffectsVolume(float value) =>
        _draft.Audio.EffectsVolume = value;

    public void SetPlanetTooltips(bool value) =>
        _draft.Gameplay.ShowPlanetTooltips = value;

    public void ResetToDefaults()
    {
        _draft.ResetToDefaults();
    }

    public void ApplyChanges()
    {
        // Сначала применяем настройки окна.
        // Если применение завершится ошибкой,
        // сохранённые настройки не изменятся.
        _applyDisplay?.Invoke(_draft.Display);

        _settings.Display = _draft.Display.CreateCopy();
        _settings.Audio = _draft.Audio.CreateCopy();
        _settings.Gameplay = _draft.Gameplay.CreateCopy();

        _draft = _settings.CreateCopy();
    }

    public void DiscardChanges()
    {
        _draft = _settings.CreateCopy();
    }
}

public sealed record ResolutionOption(
    string Label,
    int Width,
    int Height);

public sealed record SettingsViewData(
    IReadOnlyList<ResolutionOption> Resolutions,
    int SelectedResolutionIndex,
    bool Fullscreen,
    bool VSync,
    float MasterVolume,
    float MusicVolume,
    float EffectsVolume,
    bool ShowPlanetTooltips);
