using PrimeTween;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wokarol.GodConsole
{
    public class InGameConsoleLoggerView : MonoBehaviour, ILogger
    {
        [SerializeField] private InGameLogElement logElement;
        [SerializeField] private int maxLogCount = 20;
        [Space]
        [SerializeField] private RectTransform warningPopup;
        [SerializeField] private RectTransform errorPopup;

        public bool IsListening = true;
        public LogType MinimalType;

        private Queue<InGameLogElement> logQueue;

        private void Awake()
        {
            if (Application.isEditor || Debug.isDebugBuild)
            {
                MinimalType = LogType.Warning;
            }
            else
            {
                MinimalType = LogType.Error;
            }

            logQueue = new(maxLogCount);
            logElement.gameObject.SetActive(false);
            for (int i = 0; i < maxLogCount; i++)
            {
                var element = Instantiate(logElement, logElement.transform.parent);
                element.ShowWhenInactiveCallback = ShowPopupWhenConsoleHidden;

                logQueue.Enqueue(element);
            }

            Application.logMessageReceived += Application_logMessageReceived;

            warningPopup.localScale = new Vector3(1, 0, 1);
            errorPopup.localScale = new(1, 0, 1);
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= Application_logMessageReceived;
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current.f10Key.wasPressedThisFrame)
            {
                Log("Test log", LogType.Log);
                Log("Test warning", LogType.Warning);
                Log("Test error", LogType.Error);
                Log("Test assert", LogType.Assert);
                Log("Test exception", LogType.Exception);
            }
#endif
        }

        private void OnEnable()
        {
            HideWaitingPopupWhenConsoleOpens(LogType.Warning);
            HideWaitingPopupWhenConsoleOpens(LogType.Error);
        }

        public void Log(string message, LogType type = LogType.Log)
        {
            ShowLog(message, type);
        }

        public void ClearLogs()
        {
            foreach (var l in logQueue)
            {
                l.Hide();
            }
        }

        private void Application_logMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (!IsListening) return;

            var shouldAllowLogType = (type, MinimalType) switch
            {
                (_, LogType.Log) => true,
                (LogType.Warning, LogType.Warning) => true,
                // We assume all 3 error types are the same and no matter the setting they always pass
                (LogType.Error, _) => true,
                (LogType.Exception, _) => true,
                (LogType.Assert, _) => true,
                _ => false,
            };

            if (shouldAllowLogType)
                ShowLog(logString, type);
        }

        private void ShowLog(string logString, LogType type)
        {
            if (logQueue == null || logQueue.Count == 0) return;

            var element = logQueue.Dequeue();
            logQueue.Enqueue(element);

            element.Show(logString, type);
            element.transform.SetAsLastSibling();
        }

        private void ShowPopupWhenConsoleHidden(LogType type)
        {
            var popup = type switch
            {
                LogType.Warning => warningPopup,
                LogType.Error => errorPopup,
                LogType.Exception => errorPopup,
                LogType.Assert => errorPopup,
                _ => null
            };

            if (popup == null) return;

            Tween.StopAll(popup);
            Tween.ScaleY(popup, 0, 1, 0.2f, ease: Ease.OutCubic);
            Tween.ScaleY(popup, 1, 0.5f, 0.3f, startDelay: 0.2f, ease: Ease.InCubic);
        }

        private void HideWaitingPopupWhenConsoleOpens(LogType type)
        {
            var popup = type switch
            {
                LogType.Error => errorPopup,
                LogType.Warning => warningPopup,
                _ => null
            };

            if (popup == null) return;

            if (!Mathf.Approximately(popup.localScale.y, 0))
            {
                Tween.StopAll(popup);
                Tween.ScaleY(popup, popup.localScale.y, 0, 0.15f, ease: Ease.InCubic);
            }
        }
    }
}
