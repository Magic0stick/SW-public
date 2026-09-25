using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.Imperial.Medieval.Cult.Tatoo;

public sealed class TattooWindow : DefaultWindow
{
    public TattooCanvas Canvas { get; private set; }
    public Button SaveButton { get; private set; }
    public Button CancelButton { get; private set; }
    public SpriteView PlayerView { get; private set; }

    public TattooWindow(EntityUid target, IEntityManager entMan)
    {
        Title = "Ритуал Нанесения Татуировки";
        SetSize = new Vector2(400, 500);

        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        // Слой-бутерброд: внизу моделька, вверху прозрачный холст для рисования
        var layerContainer = new LayoutContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            MinSize = new Vector2(256, 256) // Стандартный хороший размер для куклы
        };

        // Настраиваем отображение модельки игрока
        PlayerView = new SpriteView
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Scale = new Vector2(4, 4), // Увеличиваем пиксель-арт, чтобы было удобно рисовать
            OverrideDirection = Direction.South // Смотрит на нас лицом
        };

        // Подгружаем спрайт персонажа
        if (entMan.TryGetComponent<SpriteComponent>(target, out var sprite))
        {
            PlayerView.SetEntity(target);
        }

        // Инициализируем наш холст 32x32
        Canvas = new TattooCanvas(PlayerView)
        {
            HorizontalExpand = true,
            VerticalExpand = true
        };

        // Добавляем их в один контейнер друг на друга
        layerContainer.AddChild(PlayerView);
        layerContainer.AddChild(Canvas);

        // Растягиваем оба контрола на всю площадь контейнера
        LayoutContainer.SetAnchorPreset(PlayerView, LayoutContainer.LayoutPreset.Wide);
        LayoutContainer.SetAnchorPreset(Canvas, LayoutContainer.LayoutPreset.Wide);

        container.AddChild(layerContainer);

        // Нижняя панель кнопок
        var buttonRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Align = BoxContainer.AlignMode.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };

        SaveButton = new Button { Text = "Завершить Ритуал", Margin = new Thickness(0, 0, 10, 0) };
        CancelButton = new Button { Text = "Прервать" };

        buttonRow.AddChild(SaveButton);
        buttonRow.AddChild(CancelButton);
        container.AddChild(buttonRow);

        Contents.AddChild(container);
    }
}
