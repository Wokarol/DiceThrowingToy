using System.Linq;
using UnityEngine;
using Wokarol.GodConsole;
using ILogger = Wokarol.GodConsole.ILogger;

namespace Wokarol
{
    public class DiceCommandsInjector : MonoBehaviour, IInjector
    {
        public void Inject(GodConsole.GodConsole.CommandBuilder b)
        {
            b.Group("dice")
                .Add("roll", (int v1, ILogger logger) =>
                {
                    var spawner = GetDiceSpawner(logger);
                    if (spawner == null) return;

                    spawner.RollDice(v1);
                })
                .Add("roll", (int v1, int v2, ILogger logger) =>
                {
                    var spawner = GetDiceSpawner(logger);
                    if (spawner == null) return;

                    spawner.RollDice(v1, v2);
                })
                .Add("roll", (int v1, int v2, int v3, ILogger logger) =>
                {
                    var spawner = GetDiceSpawner(logger);
                    if (spawner == null) return;

                    spawner.RollDice(v1, v2, v3);
                })
                .Add("roll", (int v1, int v2, int v3, int v4, ILogger logger) =>
                {
                    var spawner = GetDiceSpawner(logger);
                    if (spawner == null) return;

                    spawner.RollDice(v1, v2, v3, v4);
                })
                .Add("roll", (int v1, int v2, int v3, int v4, int v5, ILogger logger) =>
                {
                    var spawner = GetDiceSpawner(logger);
                    if (spawner == null) return;

                    spawner.RollDice(v1, v2, v3, v4, v5);
                })
                .Add("roll_repeat", (int value, int count, ILogger logger) =>
                {
                    var spawner = GetDiceSpawner(logger);
                    if (spawner == null) return;

                    spawner.RollDice(Enumerable.Repeat(value, count).ToArray());
                })
                .Add("clear", (ILogger logger) =>
                {
                    var spawner = GetDiceSpawner(logger);
                    if (spawner == null) return;

                    foreach (var dice in FindObjectsByType<Dice>(FindObjectsSortMode.None))
                    {
                        Destroy(dice.gameObject);
                    }
                });
        }

        private DiceSpawner GetDiceSpawner(ILogger logger)
        {
            var spawner = FindAnyObjectByType<DiceSpawner>();

            if (spawner == null)
            {
                logger.Log("Couldn't find a spawner", LogType.Warning);
            }

            return spawner;
        }
    }
}
