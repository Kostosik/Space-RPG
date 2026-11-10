using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;
using Engine.UI.Controls;
using GameProject.UI.Ship;

using EngineShipWindow = Engine.UI.Controls.UiWindow;

namespace GameProject.UI;

public sealed class ShipWindow
{
    private readonly EngineShipWindow _window;
    public event Action? Closed;
    public ShipWindow(UiSystem ui)
    {
        ArgumentNullException.ThrowIfNull(ui);

        _window = new EngineShipWindow(
            ui.Overlays,
            "SHIP");

        _window.Width = 760.0f;
        _window.Height = 620.0f;
        _window.MinWidth = 480.0f;
        _window.MinHeight = 360.0f;
        _window.Closed += () => Closed?.Invoke();
    }

    public bool IsOpen => _window.IsOpen;

    public void Show(
        ShipWindowViewData data,
        Vector2 position)
    {
        ArgumentNullException.ThrowIfNull(data);

        _window.SetContent(BuildContent(data));
        _window.Show(position);
    }

    public void Refresh(ShipWindowViewData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        _window.SetContent(BuildContent(data));
    }

    public void Close()
    {
        _window.Close();
    }

    private static UiWidget BuildContent(
        ShipWindowViewData data)
    {
        var scrollView = new UiScrollView
        {
            VerticalScrolling = true,
            HorizontalScrolling = false,
            ScrollSpeed = 32.0f
        };

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 10.0f
        };

        content.AddChild(BuildShipInfo(data));
        content.AddChild(BuildStats(data));
        content.AddChild(BuildSlots(data));
        content.AddChild(BuildCargo(data));

        scrollView.SetContent(content);

        return scrollView;
    }

    private static UiWidget BuildShipInfo(
        ShipWindowViewData data)
    {
        var panel = CreatePanel();

        var content = CreateSection();

        content.AddChild(new UiLabel(data.ShipName)
        {
            FontSize = 22.0f
        });

        content.AddChild(new UiLabel(
            $"CLASS: {data.ShipClass}")
        {
            FontSize = 13.0f
        });

        panel.AddChild(content);

        return panel;
    }

    private static UiWidget BuildStats(
        ShipWindowViewData data)
    {
        var panel = CreatePanel();

        var content = CreateSection();

        content.AddChild(new UiLabel("SHIP CHARACTERISTICS")
        {
            FontSize = 16.0f
        });

        content.AddChild(new UiSeparator
        {
            Length = 680.0f
        });

        content.AddChild(new UiLabel(
            $"MAX SPEED: {data.MaxSpeed:0.##}")
        {
            FontSize = 14.0f
        });

        content.AddChild(new UiLabel(
            $"FUEL CAPACITY: {data.FuelCapacity}")
        {
            FontSize = 14.0f
        });

        panel.AddChild(content);

        return panel;
    }

    private static UiWidget BuildSlots(
        ShipWindowViewData data)
    {
        var panel = CreatePanel();

        var content = CreateSection();

        content.AddChild(new UiLabel("EQUIPMENT SLOTS")
        {
            FontSize = 16.0f
        });

        content.AddChild(new UiSeparator
        {
            Length = 680.0f
        });

        if (data.Slots.Count == 0)
        {
            content.AddChild(new UiLabel("NO EQUIPMENT SLOTS"));
        }

        foreach (var slot in data.Slots)
        {
            content.AddChild(BuildSlot(slot));
        }

        panel.AddChild(content);

        return panel;
    }

    private static UiWidget BuildSlot(
        ShipSlotViewData slot)
    {
        var panel = new UiPanel
        {
            Padding = new UiThickness(8.0f),
            Background = new UiColor(34, 43, 57, 255)
        };

        var content = CreateSection();

        content.AddChild(new UiLabel(
            $"SLOT: {slot.SlotId}")
        {
            FontSize = 14.0f
        });

        content.AddChild(new UiLabel(
            $"ALLOWED: {string.Join(", ", slot.AllowedEquipmentTypes)}")
        {
            FontSize = 12.0f
        });

        if (slot.InstalledEquipment is null)
        {
            content.AddChild(new UiLabel("EMPTY")
            {
                FontSize = 13.0f,
                Color = new UiColor(170, 170, 170)
            });
        }
        else
        {
            content.AddChild(BuildEquipmentInfo(
                slot.InstalledEquipment));
        }

        panel.AddChild(content);

        return panel;
    }

    private static UiWidget BuildCargo(
        ShipWindowViewData data)
    {
        var panel = CreatePanel();

        var content = CreateSection();

        content.AddChild(new UiLabel("CARGO HOLD")
        {
            FontSize = 16.0f
        });

        content.AddChild(new UiSeparator
        {
            Length = 680.0f
        });

        if (data.Cargo.Count == 0)
        {
            content.AddChild(new UiLabel("CARGO HOLD IS EMPTY")
            {
                FontSize = 13.0f
            });
        }
        else
        {
            foreach (var equipment in data.Cargo)
            {
                content.AddChild(BuildEquipmentInfo(equipment));
            }
        }

        panel.AddChild(content);

        return panel;
    }

    private static UiWidget BuildEquipmentInfo(
        EquipmentItemViewData equipment)
    {
        var panel = new UiPanel
        {
            Padding = new UiThickness(8.0f),
            Background = new UiColor(28, 54, 65, 255)
        };

        var content = CreateSection();

        content.AddChild(new UiLabel(
            $"{equipment.Name}  |  LEVEL {equipment.Level}")
        {
            FontSize = 14.0f
        });

        content.AddChild(new UiLabel(
            $"TYPE: {equipment.Type}")
        {
            FontSize = 12.0f
        });

        content.AddChild(new UiLabel(
            $"SPEED BONUS: {equipment.Stats.MaxSpeedBonus:0.##}")
        {
            FontSize = 12.0f
        });

        content.AddChild(new UiLabel(
            $"FUEL BONUS: {equipment.Stats.FuelCapacityBonus}")
        {
            FontSize = 12.0f
        });

        panel.AddChild(content);

        return panel;
    }

    private static UiPanel CreatePanel()
    {
        return new UiPanel
        {
            Padding = new UiThickness(10.0f),
            Background = new UiColor(24, 31, 43, 255)
        };
    }

    private static UiStackPanel CreateSection()
    {
        return new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f
        };
    }
}