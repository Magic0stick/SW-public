using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using System.Collections.Generic;
using System.Numerics;

namespace Content.Client.Imperial.Medieval.Cult.Tatoo;

public sealed class TattooCanvas : Control
{
    // Теперь мы храним пиксели строго в координатах сетки от 0 до 31
    private HashSet<Vector2i> _drawnPixels = new();
    private bool _isDrawing = false;
    private readonly SpriteView _playerView;

    public TattooCanvas(SpriteView playerView)
    {
        _playerView = playerView;
        MouseFilter = MouseFilterMode.Stop;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _isDrawing = true;
            TryDrawPixel(args.RelativePosition);
            args.Handle();
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _isDrawing = false;
            args.Handle();
        }
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        if (_isDrawing)
        {
            TryDrawPixel(args.RelativePosition);
        }
    }

    private void TryDrawPixel(Vector2 localMousePos)
    {
        if (Size.X == 0 || Size.Y == 0) return;

        // 1. Переводим координаты экрана в сетку 32x32
        int pixelX = (int)(localMousePos.X / Size.X * 32f);
        int pixelY = (int)(localMousePos.Y / Size.Y * 32f);

        // Границы сетки
        if (pixelX < 0 || pixelX >= 32 || pixelY < 0 || pixelY >= 32) return;

        Vector2i targetPixel = new Vector2i(pixelX, pixelY);

        // 2. Проверка: рисуем ли мы поверх модельки персонажа?
        if (!IsHoveringCharacterModel(localMousePos))
            return; // Мимо модельки! Рисовать нельзя.

        _drawnPixels.Add(targetPixel);
    }

    private bool IsHoveringCharacterModel(Vector2 localMousePos)
    {
        // Достаем актуальный спрайт, который сейчас рендерится в SpriteView
        var sprite = _playerView.Sprite;
        if (sprite == null) return false;

        // Нам нужно проверить, есть ли видимый пиксель под мышкой.
        // Так как сложная попиксельная проверка текстур (GetPixel) из текстурного атласа
        // в UI-потоке Robust сильно бьет по производительности из-за сжатия VRAM,
        // самым надежным и быстрым способом в SS14 является проверка по bounding box
        // (границам) или проверка центральной зоны спрайта.

        // Для гуманоидов в SS14 моделька обычно сосредоточена по центру (от 0.3 до 0.7 по X)
        // и занимает от 0.15 до 0.85 по Y.
        float normX = localMousePos.X / Size.X;
        float normY = localMousePos.Y / Size.Y;

        if (normX >= 0.35f && normX <= 0.65f && normY >= 0.15f && normY <= 0.85f)
        {
            return true;
        }

        return false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        // Считаем размер одного пикселя из нашей сетки 32x32 на реальном экране
        float pixelSizeX = Size.X / 32f;
        float pixelSizeY = Size.Y / 32f;

        // Отрисовываем каждый закрашенный пиксель как закрашенный квадратик
        foreach (var pixel in _drawnPixels)
        {
            Vector2 topLeft = new Vector2(pixel.X * pixelSizeX, pixel.Y * pixelSizeY);
            Vector2 bottomRight = new Vector2((pixel.X + 1) * pixelSizeX, (pixel.Y + 1) * pixelSizeY);

            UIBox2 rect = new UIBox2(topLeft, bottomRight);

            // Рисуем пиксель татуировки (например, кроваво-красный культистский цвет)
            handle.DrawRect(rect, Color.Red.WithAlpha(0.7f));
        }
    }

    // Возвращает список закрашенных пикселей 32x32 для отправки на сервер
    public List<Vector2i> GetPixelData() => new(_drawnPixels);

    public void ClearCanvas()
    {
        _drawnPixels.Clear();
    }
}
