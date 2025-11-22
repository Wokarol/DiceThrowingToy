using UnityEngine;
using Wokarol.GameSystemsLocator;
using Wokarol.GodConsole;
using Wokarol.GodConsole.View;

namespace Wokarol.Common
{
    public class ConsoleInputBlocker : MonoBehaviour, IInjector
    {
        private object separateBlocker = new object();

        private void OnEnable()
        {
            if (TryGetComponent(out GodConsoleView view))
            {
                view.Showed += View_Showed;
                view.Hid += View_Hid;
            }
        }
        private void OnDisable()
        {
            if (TryGetComponent(out GodConsoleView view))
            {
                view.Showed -= View_Showed;
                view.Hid -= View_Hid;
            }
        }

        private void View_Showed()
        {
            GameSystems.Get<InputBlocker>().Block(this);
        }

        private void View_Hid()
        {
            GameSystems.Get<InputBlocker>().Unlock(this);
        }

        public void Inject(Wokarol.GodConsole.GodConsole.CommandBuilder b)
        {
            var blockerGroup = b.Group("input blocker");

            blockerGroup
                .Add("count", (InputBlocker blocker, Wokarol.GodConsole.ILogger logger) => logger.Log($"Input blocked by {blocker.Count} objects"));

            blockerGroup.Group("console")
                .Add("block", (InputBlocker blocker, Wokarol.GodConsole.ILogger logger) =>
                {
                    blocker.Block(this);
                    logger.Log($"Blocked input");
                })
                .Add("unblock", (InputBlocker blocker, Wokarol.GodConsole.ILogger logger) =>
                {
                    blocker.Unlock(this);
                    logger.Log($"Unblocked input");
                });

            blockerGroup.Group("separate")
                .Add("block", (InputBlocker blocker, Wokarol.GodConsole.ILogger logger) =>
                {
                    blocker.Block(separateBlocker);
                    logger.Log($"Blocked input using separate blocker");
                })
                .Add("unblock", (InputBlocker blocker, Wokarol.GodConsole.ILogger logger) =>
                {
                    blocker.Unlock(separateBlocker);
                    logger.Log($"Unblocked input using separate blocker");
                });
        }
    }
}
