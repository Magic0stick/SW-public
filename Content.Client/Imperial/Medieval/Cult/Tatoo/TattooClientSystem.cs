using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Network;
using Content.Shared.Imperial.Medieval.Cult.Tatoo;

namespace Content.Client.Imperial.Medieval.Cult.Tatoo;

public sealed class TattooClientSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public void OpenTattooWindow(EntityUid user, EntityUid target)
    {
        var window = new TattooWindow();

        _uiManager.WindowRoot.AddChild(window);
        window.OpenCentered();

        window.SaveButton.OnPressed += (args) => {
            var connections = window.Canvas.GetConnections();
            var connectionList = new List<TattooConnection>();
            foreach (var conn in connections)
            {
                connectionList.Add(new TattooConnection(conn.Item1, conn.Item2));
            }

            var message = new SubmitTattooCircuitMessage
            {
                Target = EntityManager.GetNetEntity(target),
                Connections = connectionList,
                DrawingData = window.Canvas.GetDrawingData()
            };

            EntityManager.EntityNetManager?.SendSystemNetworkMessage(message);

            window.Close();
        };
        window.CancelButton.OnPressed += (args) => {
            window.Close();
        };
    }
}
