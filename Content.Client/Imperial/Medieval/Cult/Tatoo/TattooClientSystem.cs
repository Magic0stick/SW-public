using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Content.Shared.Imperial.Medieval.Cult.Tatoo;
using System.Collections.Generic;

namespace Content.Client.Imperial.Medieval.Cult.Tatoo;

public sealed class TattooClientSystem : EntitySystem
{
    // В RobustManager зависимости объявляются через [Dependency]
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    // Храним ссылку на текущее открытое окно, чтобы избежать дубликатов и утечек памяти
    private TattooWindow? _currentWindow;

    public override void Initialize()
    {
        base.Initialize();
        // Подписка на сетевое событие от сервера
        SubscribeNetworkEvent<OpenTattooWindowMessage>(OnOpenTattooWindow);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        // Обязательно подчищаем UI при выгрузке системы
        CloseTattooWindow();
    }

    private void OnOpenTattooWindow(OpenTattooWindowMessage message)
    {
        Log.Info($"[TattooDebug] Received OpenTattooWindowMessage. Target NetEntity: {message.Target}");

        // Безопасный резолв сущности
        if (!TryGetEntity(message.Target, out var target))
        {
            Log.Warning($"[TattooDebug] Failed to resolve NetEntity {message.Target} to local EntityUid");
            return;
        }

        Log.Info($"[TattooDebug] Opening Tattoo Window for target: {target.Value}");
        OpenTattooWindow(target.Value);
    }

    public void OpenTattooWindow(EntityUid target)
    {
        CloseTattooWindow();

        // Передаем target в конструктор окна
        _currentWindow = new TattooWindow(target, EntityManager);
        _currentWindow.OpenCentered();

        _currentWindow.SaveButton.OnPressed += _ => OnSavePressed(target);
        _currentWindow.CancelButton.OnPressed += _ => CloseTattooWindow();
    }


    private void OnSavePressed(EntityUid target)
    {
        if (_currentWindow == null)
            return;

        Log.Info($"[TattooDebug] Save button pressed. Sending SubmitTattooCircuitMessage for target: {target}");

        // 1. Получаем пиксельные данные 32x32 напрямую из канваса
        var pixels = _currentWindow.Canvas.GetPixelData();

        // 2. Формируем новое сообщение для сервера
        var message = new SubmitTattooCircuitMessage
        {
            Target = GetNetEntity(target),
            // Передаем список закрашенных пикселей (Vector2i)
            Pixels = pixels
        };

        // 3. Отправляем на сервер
        RaiseNetworkEvent(message);

        Log.Info($"[TattooDebug] SubmitTattooCircuitMessage sent. Pixels count: {pixels.Count}");

        CloseTattooWindow();
    }


    private void CloseTattooWindow()
    {
        if (_currentWindow == null)
            return;

        // Закрываем UI элемент и полностью освобождаем его ресурсы (Dispose)
        _currentWindow.Close();
        _currentWindow.Dispose();
        _currentWindow = null;

        Log.Info($"[TattooDebug] Tattoo Window closed and disposed.");
    }
}
