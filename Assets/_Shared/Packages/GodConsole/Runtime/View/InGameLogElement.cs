using PrimeTween;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wokarol.GodConsole
{
    public class InGameLogElement : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image indicator;
        [Space]
        [SerializeField] private Color infoColor;
        [SerializeField] private Color warnColor;
        [SerializeField] private Color errorColor;

        public Action<LogType> ShowWhenInactiveCallback;

        public void Show(string message, LogType type)
        {
            gameObject.SetActive(true);

            label.text = message;

            indicator.color = type switch
            {
                LogType.Log => infoColor,
                LogType.Warning => warnColor,
                LogType.Assert => errorColor,
                LogType.Exception => errorColor,
                LogType.Error => errorColor,
                _ => infoColor
            };

            if (gameObject.activeInHierarchy)
            {
                Tween.Alpha(group, 0f, 1f, 0.4f);
            }
            else
            {
                group.alpha = 1;
                ShowWhenInactiveCallback(type);
            }
        }

        public void Hide()
        {
            Tween.StopAll(group);

            if (group.alpha != 0)
            {
                if (gameObject.activeInHierarchy)
                {
                    Tween.Alpha(group, group.alpha, 0f, 0.3f);
                }
                else
                {
                    group.alpha = 0;
                }
            }
        }
    }
}
