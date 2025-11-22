using System;
using UnityEngine;

namespace Wokarol.Utils
{
    public static class LogExtras
    {
        public static string Prefix(Component component)
        {
            return $"{Tick()} {Tag(GetNameFromType(component.GetType()))}";
        }

        public static string Prefix(string name)
        {
            return $"{Tick()} {Tag(name)}";
        }

        public static string Value<T>(T v)
        {
            return $"<color=green>{v}</color>";
        }

        public static string Value(float v, string format)
        {
            return $"<color=green>{v.ToString(format)}</color>";
        }

        public static string ValuePercent(float v, string format = null)
        {
            return $"<color=green>{(v * 100f).ToString(format ?? "F0")}%</color>";
        }

        public static string Tag(string tag)
        {
            return $"<color=orange>[{tag}]</color>";
        }
        private static string GetNameFromType(Type type)
        {
            return type.Name;
        }
        public static string Tick()
        {
            return $"<color=#00FFFF>[F: {Time.frameCount,5}]</color>";
        }
    }
}
