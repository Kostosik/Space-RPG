
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using GameProject.Settings;

namespace GameProject.UI.Settings;

public sealed class SettingsWindow : IDisposable
{
    private readonly UiSystem _ui;
    private readonly SettingsPresenter _presenter;
    private readonly UiWindow _window;

    private bool _disposed;

    public SettingsWindow(
        UiSystem ui,
        SettingsPresenter presenter)
    {
        ArgumentNullException.ThrowIfNull(ui);
        ArgumentNullException.ThrowIfNull(presenter);

        _ui = ui;
        _presenter = presenter;

        _window = new UiWindow(
            ui.Overlays,
            "GAME SETTINGS")
        {
            Width = 760.0f,
            Height = 640.0f,
            MinWidth = 760.0f,
            MinHeight = 640.0f,
            Resizable = false
        };

        _window.Closed += OnWindowClosed;
    }

    public bool IsOpen => _window.IsOpen;

    public void Show()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _presenter.BeginEdit();
        RebuildContent();

        // Центрируем окно относительно стандартного экрана 1280x720.
        _window.Show(new Vector2(260.0f, 40.0f));
    }

    private void RebuildContent()
    {
        var data = _presenter.GetViewData();

        var scrollView = new UiScrollView
        {
            Width = 710.0f,
            Height = 500.0f,
            VerticalScrolling = true,
            HorizontalScrolling = false,
            ShowHorizontalScrollBar = false,
            ScrollSpeed = 32.0f
        };

        var sections = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 12.0f,
            Width = 690.0f
        };

        sections.AddChild(BuildDisplaySection(data));
        sections.AddChild(BuildAudioSection(data));
        sections.AddChild(BuildGameplaySection(data));

        scrollView.SetContent(sections);

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 12.0f,
            Width = 720.0f,
            Height = 560.0f
        };

        content.AddChild(scrollView);
        content.AddChild(BuildFooter());

        _window.SetContent(content);
    }

    private UiPanel BuildDisplaySection(
        SettingsViewData data)
    {
        var section = CreateSection("DISPLAY");

        var resolutionRow = CreateRow("RESOLUTION");

        var resolution = new UiDropdown(_ui.Overlays)
        {
            Width = 260.0f,
            Height = 42.0f
        };

        foreach (var option in data.Resolutions)
        {
            resolution.AddOption(option.Label);
        }

        resolution.SetSelectedIndex(
            data.SelectedResolutionIndex);

        resolution.SelectionChanged += (index, _) =>
        {
            _presenter.SetResolution(index);
        };

        resolutionRow.AddChild(resolution);
        section.AddChild(resolutionRow);

        var toggles = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 20.0f
        };

        var fullscreen = new UiToggle("FULLSCREEN")
        {
            Width = 200.0f
        };

        fullscreen.SetValue(data.Fullscreen);
        fullscreen.ValueChanged += _presenter.SetFullscreen;

        var vsync = new UiToggle("VERTICAL SYNC")
        {
            Width = 200.0f
        };

        vsync.SetValue(data.VSync);
        vsync.ValueChanged += _presenter.SetVSync;

        toggles.AddChild(fullscreen);
        toggles.AddChild(vsync);

        section.AddChild(toggles);

        return section;
    }

    private UiPanel BuildAudioSection(
        SettingsViewData data)
    {
        var section = CreateSection("AUDIO");

        section.AddChild(BuildVolumeRow(
            "MASTER VOLUME",
            data.MasterVolume,
            _presenter.SetMasterVolume));

        section.AddChild(BuildVolumeRow(
            "MUSIC VOLUME",
            data.MusicVolume,
            _presenter.SetMusicVolume));

        section.AddChild(BuildVolumeRow(
            "EFFECTS VOLUME",
            data.EffectsVolume,
            _presenter.SetEffectsVolume));

        return section;
    }

    private UiWidget BuildVolumeRow(
        string title,
        float value,
        Action<float> onChanged)
    {
        var row = CreateRow(title);

        var slider = new UiSlider
        {
            Width = 300.0f,
            Height = 32.0f,
            Value = value
        };

        var valueLabel = new UiLabel(
            $"{value * 100.0f:0}%")
        {
            Width = 55.0f,
            Height = 32.0f,
            FontSize = 14.0f
        };

        slider.ValueChanged += newValue =>
        {
            onChanged(newValue);
            valueLabel.Text = $"{newValue * 100.0f:0}%";
        };

        row.AddChild(slider);
        row.AddChild(valueLabel);

        return row;
    }

    private UiPanel BuildGameplaySection(
        SettingsViewData data)
    {
        var section = CreateSection("GAMEPLAY");

        var tooltips = new UiToggle("PLANET TOOLTIPS")
        {
            Width = 240.0f
        };

        tooltips.SetValue(data.ShowPlanetTooltips);
        tooltips.ValueChanged += _presenter.SetPlanetTooltips;

        section.AddChild(tooltips);

        return section;
    }

    private UiStackPanel BuildFooter()
    {
        var footer = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 12.0f
        };

        var reset = new UiButton("RESET DEFAULTS")
        {
            Width = 180.0f,
            Height = 42.0f
        };

        reset.Clicked += () =>
        {
            _presenter.ResetToDefaults();
            RebuildContent();
        };

        var apply = new UiButton("APPLY")
        {
            Width = 120.0f,
            Height = 42.0f
        };

        apply.Clicked += () =>
        {
            _presenter.ApplyChanges();
            _window.Close();
        };

        var cancel = new UiButton("CANCEL")
        {
            Width = 120.0f,
            Height = 42.0f
        };

        cancel.Clicked += () =>
        {
            _presenter.DiscardChanges();
            _window.Close();
        };

        footer.AddChild(reset);
        footer.AddChild(apply);
        footer.AddChild(cancel);

        return footer;
    }

    private static UiPanel CreateSection(string title)
    {
        var panel = new UiPanel
        {
            Padding = new UiThickness(12.0f),
            Background = new UiColor(24, 31, 43, 255),
            Width = 690.0f
        };

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 10.0f
        };

        content.AddChild(new UiLabel(title)
        {
            FontSize = 18.0f,
            Height = 26.0f
        });

        content.AddChild(new UiSeparator
        {
            Length = 650.0f
        });

        panel.AddChild(content);

        return panel;
    }

    private static UiStackPanel CreateRow(string title)
    {
        var row = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 12.0f,
            Height = 42.0f
        };

        row.AddChild(new UiLabel(title)
        {
            Width = 190.0f,
            Height = 36.0f,
            FontSize = 14.0f
        });

        return row;
    }

    private void OnWindowClosed()
    {
        _presenter.DiscardChanges();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _window.Closed -= OnWindowClosed;
        _window.Close();
    }
}
