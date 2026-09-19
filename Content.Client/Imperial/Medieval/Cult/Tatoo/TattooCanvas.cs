using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Content.Shared.Imperial.Medieval.Cult.Tatoo;
using System.Collections.Generic;
using System.Numerics;

namespace Content.Client.Imperial.Medieval.Cult.Tatoo;

public sealed class TattooCanvas : Control
{
    private List<Vector2> _pixels = new();
    private bool _isDrawing = false;
    private TattooSlot? _lastActivatedSlot = null;
    private HashSet<(TattooSlot, TattooSlot)> _activeConnections = new();

    private readonly Dictionary<TattooSlot, Vector2> _slotPositions = new()
    {
        { TattooSlot.Head, new Vector2(0.5f, 0.1f) },
        { TattooSlot.TorsoUpper, new Vector2(0.5f, 0.3f) },
        { TattooSlot.TorsoLower, new Vector2(0.5f, 0.5f) },
        { TattooSlot.ArmLeft, new Vector2(0.3f, 0.35f) },
        { TattooSlot.ArmRight, new Vector2(0.7f, 0.35f) },
        { TattooSlot.LegLeft, new Vector2(0.4f, 0.7f) },
        { TattooSlot.LegRight, new Vector2(0.6f, 0.7f) }
    };

    public TattooCanvas() { }

    // В RobustToolbox Control не имеет виртуальных методов OnMouseDown.
    // Мы будем использовать события клика/движения, либо методы из базового класса,
    // если они доступны. Для реализации рисования мы создадим методы-обработчики,
    // которые будут вызываться извне или через подписку.

    public void HandleMouseDown(Vector2 position)
    {
        _isDrawing = true;
        AddPixel(position);
    }

    public void HandleMouseMove(Vector2 position)
    {
        if (_isDrawing)
        {
            AddPixel(position);
        }
    }

    public void HandleMouseUp()
    {
        _isDrawing = false;
    }

    private void AddPixel(Vector2 pos)
    {
        Vector2 normalizedPos = new Vector2(pos.X / 500f, pos.Y / 800f);
        _pixels.Add(normalizedPos);

        foreach (var slot in _slotPositions)
        {
            if (Vector2.Distance(normalizedPos, slot.Value) < 0.02f)
            {
                ActivateSlot(slot.Key);
                break;
            }
        }
    }

    private void ActivateSlot(TattooSlot slot)
    {
        if (_lastActivatedSlot.HasValue && _lastActivatedSlot.Value != slot)
        {
            _activeConnections.Add((_lastActivatedSlot.Value, slot));
            _activeConnections.Add((slot, _lastActivatedSlot.Value));
        }
        _lastActivatedSlot = slot;
    }

    public List<(TattooSlot, TattooSlot)> GetConnections() => new(_activeConnections);
    public List<Vector2> GetDrawingData() => _pixels;
}
