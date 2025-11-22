using UnityEngine;

namespace Wokarol.GodConsole
{
    public class InGameLoggerCommandsInjector : MonoBehaviour, IInjector
    {
        [SerializeField] private InGameConsoleLoggerView loggerView = null;

        public void Inject(GodConsole.CommandBuilder b)
        {
            b.Add("clear", () =>
            {
                loggerView.ClearLogs();
            });

            b.Group("logger")
                .Add("level", (ILogger logger) =>
                {
                    logger.Log($"Log level is set to {loggerView.MinimalType}");

                })
                .Add("level", (string logLevel, ILogger logger) =>
                {
                    if (!loggerView.IsListening)
                    {
                        logger.Log("Logger is not listening. Execute \"logger enable t\" to enable it", LogType.Warning);
                    }

                    var logType = logLevel switch
                    {
                        "log" => LogType.Log,
                        "info" => LogType.Log,
                        "warning" => LogType.Warning,
                        "warn" => LogType.Warning,
                        "error" => LogType.Error,
                        "err" => LogType.Error,
                        _ => (LogType)(-1)
                    };

                    if ((int)logType == -1)
                    {
                        logger.Log("Failed to find matching log level, options are: log, warn, error", LogType.Error);
                        return;
                    }

                    loggerView.MinimalType = logType;

                    logger.Log($"Set the log level to {logType}");

                })
                .Add("enable", (ILogger logger) =>
                {
                    logger.Log($"In-game logger is {(loggerView.IsListening ? "enabled" : "disabled")}");
                })
                .Add("enable", (bool enable, ILogger logger) =>
                {
                    if (enable)
                    {
                        loggerView.IsListening = true;
                        logger.Log($"Enabled in-game logger");
                    }
                    else
                    {
                        logger.Log("Disabled in-game logger");
                        loggerView.IsListening = false;
                    }
                });
        }
    }
}
