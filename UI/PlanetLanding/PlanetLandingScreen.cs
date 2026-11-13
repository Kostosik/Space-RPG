using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using Engine.UI.Screens;

namespace GameProject.UI.PlanetLanding;

public sealed class PlanetLandingScreen : UiScreen
{
    private readonly UiPanel _viewHost;

    private readonly UiStackPanel _navigation;

    private readonly Dictionary<string, IPlanetLandingView> _views =
        new(StringComparer.Ordinal);

    private readonly Dictionary<string, UiButton> _navigationButtons =
        new(StringComparer.Ordinal);

    private IPlanetLandingView? _activeView;

    public string? ActiveViewId => _activeView?.Id;

    public PlanetLandingScreen(
        PlanetLandingViewData data,
        IEnumerable<IPlanetLandingView> views,
        Action takeOff)
        : base(CreateRoot())
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(views);
        ArgumentNullException.ThrowIfNull(takeOff);

        _viewHost = new UiPanel
        {
            Width = 850.0f,
            Height = 460.0f,
            Padding = new UiThickness(18.0f),
            Background = new UiColor(12, 25, 40, 255)
        };

        _navigation = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f
        };

        var layout = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 12.0f
        };

        layout.AddChild(BuildHeader(data));

        var body = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 14.0f,
            Height = 460.0f
        };

        body.AddChild(_viewHost);
        body.AddChild(BuildLocationPanel(data));

        layout.AddChild(body);
        layout.AddChild(BuildFooter(takeOff));

        ((UiPanel)Root).AddChild(layout);

        foreach (var view in views)
        {
            RegisterView(view);
        }

        if (_views.Count == 0)
        {
            throw new InvalidOperationException(
                "Planet landing screen requires at least one view.");
        }
    }

    public void RegisterView(
        IPlanetLandingView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        ArgumentException.ThrowIfNullOrWhiteSpace(view.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(view.Title);

        ArgumentNullException.ThrowIfNull(view.Content);

        if (view.Content.Parent is not null)
        {
            throw new InvalidOperationException(
                "Planet landing view content already has a parent.");
        }

        if (!_views.TryAdd(view.Id, view))
        {
            throw new InvalidOperationException(
                $"Planet landing view '{view.Id}' is already registered.");
        }

        var viewId = view.Id;

        var button = new UiButton(view.Title)
        {
            Width = 160.0f,
            Height = 46.0f,
            FontSize = 14.0f
        };

        button.Clicked += () => SelectView(viewId);

        _navigationButtons.Add(viewId, button);
        _navigation.AddChild(button);

        if (_activeView is null)
        {
            SelectView(viewId);
        }
    }

    public bool SelectView(string viewId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewId);

        if (!_views.TryGetValue(viewId, out var view))
        {
            return false;
        }

        if (ReferenceEquals(_activeView, view))
        {
            return true;
        }

        if (_activeView is not null)
        {
            _viewHost.RemoveChild(
                _activeView.Content);
        }

        _activeView = view;

        _viewHost.AddChild(
            view.Content);

        UpdateNavigationState();

        return true;
    }

    private void UpdateNavigationState()
    {
        if (_activeView is null)
        {
            return;
        }

        foreach (var pair in _navigationButtons)
        {
            var title = _views[pair.Key].Title;

            pair.Value.Text =
                pair.Key == _activeView.Id
                    ? $"[{title}]"
                    : title;
        }
    }

    private static UiPanel CreateRoot()
    {
        return new UiPanel
        {
            Background = new UiColor(4, 10, 24, 255),
            Padding = new UiThickness(28.0f)
        };
    }

    private static UiWidget BuildHeader(
        PlanetLandingViewData data)
    {
        var panel = new UiPanel
        {
            Height = 76.0f,
            Padding = new UiThickness(16.0f),
            Background = new UiColor(20, 48, 68, 255)
        };

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 18.0f
        };

        content.AddChild(new UiLabel(
            data.LocationName.ToUpperInvariant())
        {
            FontSize = 28.0f,
            Color = new UiColor(235, 245, 255, 255)
        });

        content.AddChild(new UiLabel(
            data.LocationType.ToUpperInvariant())
        {
            FontSize = 14.0f,
            Color = new UiColor(110, 205, 235, 255),
            VerticalAlignment = UiVerticalAlignment.Center
        });

        content.AddChild(new UiLabel("LANDING CONFIRMED")
        {
            FontSize = 13.0f,
            Color = new UiColor(110, 230, 165, 255),
            VerticalAlignment = UiVerticalAlignment.Center
        });

        panel.AddChild(content);

        return panel;
    }

    private static UiPanel BuildLocationPanel(
        PlanetLandingViewData data)
    {
        var panel = new UiPanel
        {
            Width = 340.0f,
            Height = 460.0f,
            Padding = new UiThickness(18.0f),
            Background = new UiColor(18, 34, 50, 255)
        };

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 12.0f
        };

        content.AddChild(new UiLabel("LOCATION DATA")
        {
            FontSize = 18.0f,
            Color = new UiColor(115, 205, 235, 255)
        });

        content.AddChild(CreateDivider());

        content.AddChild(new UiLabel("OBJECT TYPE")
        {
            FontSize = 13.0f,
            Color = new UiColor(135, 165, 185, 255)
        });

        content.AddChild(new UiLabel(data.LocationType)
        {
            FontSize = 20.0f,
            Color = new UiColor(235, 245, 255, 255)
        });

        content.AddChild(CreateDivider());

        content.AddChild(new UiLabel("SHIP STATUS")
        {
            FontSize = 13.0f,
            Color = new UiColor(135, 165, 185, 255)
        });

        content.AddChild(new UiLabel("LANDED")
        {
            FontSize = 20.0f,
            Color = new UiColor(110, 230, 165, 255)
        });

        panel.AddChild(content);

        return panel;
    }

    private UiCanvas BuildFooter(
        Action takeOff)
    {
        var footer = new UiCanvas
        {
            Height = 60.0f
        };

        _navigation.LayoutMode = UiLayoutMode.Anchor;
        _navigation.Anchor = UiAnchor.TopLeft;
        _navigation.Offset = new Vector2(0.0f, 8.0f);
        _navigation.Width = 720.0f;
        _navigation.Height = 46.0f;

        footer.AddChild(_navigation);

        var takeOffButton = new UiButton("TAKE OFF")
        {
            Width = 180.0f,
            Height = 46.0f,
            FontSize = 16.0f,
            LayoutMode = UiLayoutMode.Anchor,
            Anchor = UiAnchor.TopRight,
            Offset = new Vector2(0.0f, 8.0f)
        };

        takeOffButton.Clicked += takeOff;

        footer.AddChild(takeOffButton);

        return footer;
    }

    private static UiPanel CreateDivider()
    {
        return new UiPanel
        {
            Height = 1.0f,
            Background = new UiColor(55, 90, 115, 255)
        };
    }
}