using System;
using UnityEngine;

namespace Wokarol.GodConsole
{
    public interface IServiceProvider
    {
        object Get(Type type);
    }

    public interface IInjector
    {
        void Inject(GodConsole.CommandBuilder b);
    }

    public interface ILogger
    {
        void Log(string message, LogType type = LogType.Log);
    }

    public class ConsoleLoggerFallback : ILogger
    {
        public void Log(string message, LogType type = LogType.Log)
        {
            Debug.unityLogger.Log(type, message);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(string route)
        {
            Route = route;
        }

        public string Route { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public class CommandRouteAttribute : Attribute
    {
        public CommandRouteAttribute(string route)
        {
            Route = route;
        }

        public string Route { get; set; }
    }
}
