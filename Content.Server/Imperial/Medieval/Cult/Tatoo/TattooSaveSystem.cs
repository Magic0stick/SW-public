using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Content.Shared.Imperial.Medieval.Cult.Tatoo;
using Content.Shared.Interaction; // Для проверки дистанции взаимодействия

namespace Content.Server.Imperial.Medieval.Cult.Tatoo;

public sealed class TattooSaveSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        // В Robust сервер подписывается на сетевые сообщения систем через SubscribeNetworkEvent
        SubscribeNetworkEvent<SubmitTattooCircuitMessage>(OnSaveTattoo);
    }

    private void OnSaveTattoo(SubmitTattooCircuitMessage message, EntitySessionEventArgs args)
    {
        Log.Info($"[TattooDebug] Received SubmitTattooCircuitMessage. Target NetEntity: {message.Target}");

        // 1. Проверяем, существует ли сессия игрока и его персонаж на сервере
        if (args.SenderSession.AttachedEntity is not { } userUid)
        {
            Log.Warning("[TattooDebug] Network message received from a session with no attached entity.");
            return;
        }

        // 2. Безопасно резолвим цель
        if (!TryGetEntity(message.Target, out var targetUid))
        {
            Log.Warning($"[TattooDebug] Failed to resolve NetEntity {message.Target} to local EntityUid");
            return;
        }

        // 3. ЗАЩИТА ОТ ЧИТОВ: Проверяем, находится ли рисовальщик достаточно близко к цели
        // Задаем стандартную дистанцию клика (InRangeUnobstructed проверяет стены/окна на пути)
        if (!_interactionSystem.InRangeUnobstructed(userUid, targetUid.Value, range: SharedInteractionSystem.InteractionRange))
        {
            Log.Warning($"[TattooDebug] Player {userUid} tried to tattoo {targetUid} out of range!");
            return;
        }

        Log.Info($"[TattooDebug] Saving tattoo for target: {targetUid}. Pixels count: {message.Pixels.Count}");

        // 4. Получаем или создаем компонент татуировки на цели
        var tattooComp = EnsureComp<TattooComponent>(targetUid.Value);

        // 5. Перезаписываем данные. Никакого JSON! Сохраняем напрямую сетку точек.
        // Подразумевается, что класс TattooComponent в Shared/Server содержит поле:
        // public List<Vector2i> TattooPixels = new();
        tattooComp.TattooPixels.Clear();
        tattooComp.TattooPixels.AddRange(message.Pixels);

        // Маркируем компонент как "измененный", чтобы Robust синхронизировал изменения,
        // если компонент имеет флаг [NetworkedComponent]
        Dirty(targetUid.Value, tattooComp);

        Log.Info($"[TattooDebug] Tattoo successfully saved to entity {targetUid}");
    }
}
