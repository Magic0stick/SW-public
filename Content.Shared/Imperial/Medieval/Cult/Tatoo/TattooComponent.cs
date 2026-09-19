using Robust.Shared;

namespace Content.Shared.Imperial.Medieval.Cult.Tatoo;

/// <summary>
/// Компонент, хранящий текущие татуировки-схемы на теле персонажа.
/// </summary>
[RegisterComponent]
public sealed partial class TattooComponent : Component
{
    // Список всех нанесенных схем.
    // Обычно одна большая схема на все тело, но архитектурно оставим список.
    public List<TattooCircuit> Circuits = new();
}
