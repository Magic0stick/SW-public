using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared;
using Content.Shared.Imperial.Medieval.Cult.Tatoo;

namespace Content.Client.Imperial.Medieval.Cult.Tatoo;

public sealed class TattooWindow : DefaultWindow
{
    public TattooCanvas Canvas { get; private set; }
    public Button SaveButton { get; private set; }
    public Button CancelButton { get; private set; }

    public TattooWindow()
    {
        Title = "Ритуал Нанесения Татуировки";

        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };

        Canvas = new TattooCanvas();
        container.AddChild(Canvas);

        var buttonRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Align = BoxContainer.AlignMode.Center
        };

        SaveButton = new Button { Text = "Завершить Ритуал" };
        CancelButton = new Button { Text = "Прервать" };

        buttonRow.AddChild(SaveButton);
        buttonRow.AddChild(CancelButton);
        container.AddChild(buttonRow);

        // В DefaultWindow Contents — это свойство только для чтения,
        // поэтому добавляем корневой контейнер через AddChild
        AddChild(container);
    }
}
