using PrimeTween;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Wokarol.GodConsole.View
{
    public class GodConsoleView : MonoBehaviour
    {
        const string historyPlayerPrefsKey = "{D4663061-FF61-4563-ABC0-B112D48B9BB1}";

        [SerializeField] private GodConsole console;
        [SerializeField] private CanvasGroup canvas;
        [SerializeField] private TMP_InputField inputField;
        [Space]
        [SerializeField] private InputAction openAction = new("Open Console");
        [SerializeField] private InputAction useSuggestionAction = new("Fill from suggestions");
        [SerializeField] private InputAction nextAction = new("Next on list");
        [SerializeField] private InputAction prevAction = new("Previous on list");
        [Space]
        [SerializeField] private ListedSuggestionElement suggestionElement;
        [SerializeField] private TMP_Text suggestionEcho;
        [SerializeField] private int maxSuggestionCount;
        [SerializeField] private int maxHistoryCount;

        private bool state = false;
        private List<string> suggestions = new();
        private List<ListedSuggestionElement> listedSuggestions;
        private int suggestionIndex = -1;
        private int historyIndex = -1;
        private string commandBeforeEnteringHistory;

        private bool externalOpenAction;
        private bool externalUseSuggestionAction;
        private bool externalSubmitAction;
        private bool externalNextAction;
        private bool externalPrevAction;

        private List<string> history = new();

        public event Action Showed;
        public event Action Hid;

        private void Start()
        {
            canvas.alpha = 0;
            canvas.gameObject.SetActive(false);
            state = false;

            openAction.Enable();
            useSuggestionAction.Enable();
            nextAction.Enable();
            prevAction.Enable();

            inputField.onSubmit.AddListener(SubmitCommand);
            inputField.onDeselect.AddListener(OnDeselectInput);
            inputField.onEndEdit.AddListener(OnDeselectInput);
            inputField.onValueChanged.AddListener(OnInputFieldValueChanged);

            inputField.onValidateInput = ValidateInput;

            listedSuggestions = new(maxSuggestionCount);
            suggestionElement.gameObject.SetActive(false);
            for (int i = 0; i < maxSuggestionCount; i++)
            {
                var suggestionElementInstance = Instantiate(suggestionElement, suggestionElement.transform.parent);
                listedSuggestions.Add(suggestionElementInstance);
            }

            suggestionEcho.text = "";

            history = JsonUtility.FromJson<WrapperForStringList>(PlayerPrefs.GetString(historyPlayerPrefsKey, "{\"list\": []}")).list;
        }

        private void OnDestroy()
        {
            PlayerPrefs.SetString(historyPlayerPrefsKey, JsonUtility.ToJson(new WrapperForStringList(history)));
        }

        private void Update()
        {
            HandleOpening();

            if (state)
            {
                HandleFillingSuggestionInput();
                HandleHistoryAndSuggestionInput();

                if (externalSubmitAction)
                {
                    SubmitCommand(null);
                }
            }

            externalOpenAction = false;
            externalUseSuggestionAction = false;
            externalSubmitAction = false;
            externalNextAction = false;
            externalPrevAction = false;
        }

        public void TriggerOpenAction() => externalOpenAction = true;
        public void TriggerUseSuggestionAction()
        {
            externalUseSuggestionAction = true;
            ForceSelection();
            StartCoroutine(UpdateCaretPositionAfterSingleFrame());
        }

        public void TriggerSubmitAction() => externalSubmitAction = true;
        public void TriggerNextAction() => externalNextAction = true;
        public void TriggerPrevAction() => externalPrevAction = true;

        private void HandleFillingSuggestionInput()
        {
            if ((useSuggestionAction.WasPerformedThisFrame() || externalUseSuggestionAction) && suggestions.Count > 0)
            {
                int index = suggestionIndex;
                if (suggestionIndex == -1)
                {
                    index = 0;
                }

                inputField.text = suggestions[index] + " ";
                inputField.caretPosition = inputField.text.Length;

            }
        }

        private void HandleOpening()
        {
            if (openAction.WasPerformedThisFrame() || externalOpenAction)
            {
                state = !state;

                if (state)
                {
                    OnInputFieldValueChanged(inputField.text);

                    Tween.StopAll(canvas);
                    canvas.gameObject.SetActive(true);
                    Tween.Alpha(canvas, 1f, 0.4f, useUnscaledTime: true)
                        .OnComplete(() =>
                        {
                            ForceSelection();
                        });

                    Showed?.Invoke();
                }
                else
                {
                    var eventSystem = EventSystem.current;
                    if (eventSystem == null)
                    {
                        Debug.LogError("There is no Event System present in the scene. The console uses Unity's UI and requires an Event System present.");
                        return;
                    }

                    eventSystem.SetSelectedGameObject(null);

                    Tween.StopAll(canvas);
                    Tween.Alpha(canvas, 0f, 0.4f, useUnscaledTime: true)
                        .OnComplete(() =>
                        {
                            inputField.text = string.Empty;
                            canvas.gameObject.SetActive(false);
                        });

                    Hid?.Invoke();
                }
            }
        }

        private void HandleHistoryAndSuggestionInput()
        {
            if (nextAction.WasPerformedThisFrame() || externalNextAction)
            {
                if (historyIndex == -1 && suggestions.Count > 0)
                {
                    suggestionIndex++;
                    if (suggestionIndex >= suggestions.Count)
                    {
                        suggestionIndex--;
                    }
                    UpdateSuggestionSelection();
                }
                else
                {
                    historyIndex--;
                    if (historyIndex < -1)
                    {
                        historyIndex++;
                    }
                    else
                    {
                        SetCommandFromHistory();
                    }
                }
            }

            if (prevAction.WasPerformedThisFrame() || externalPrevAction)
            {
                if (suggestionIndex == -1)
                {
                    if (historyIndex == history.Count - 1)
                        return;

                    if (historyIndex == -1)
                    {
                        commandBeforeEnteringHistory = inputField.text;
                    }

                    historyIndex++;
                    SetCommandFromHistory();
                }
                else if (suggestions.Count > 0)
                {
                    suggestionIndex--;
                    if (suggestionIndex < -1)
                        suggestionIndex++;
                    UpdateSuggestionSelection();
                }
            }
        }

        private char ValidateInput(string text, int charIndex, char addedChar)
        {
            if (addedChar == ' ')
            {
                if (text.Length == 0) return '\0';
                if (text[^1] == ' ') return '\0';
            }

            bool isLowercase = addedChar >= 'a' && addedChar <= 'z';
            bool isUppercase = addedChar >= 'A' && addedChar <= 'Z';
            bool isDigit = addedChar >= '0' && addedChar <= '9';
            bool isSpecial = addedChar == ' ' || addedChar == '_' || addedChar == '-' || addedChar == '.';

            return isLowercase || isUppercase || isDigit || isSpecial
                ? addedChar
                : '\0';
        }

        private void SubmitCommand(string _)
        {
            if (!state) return;

            if (suggestionIndex == -1)
            {
                console.Execute(inputField.text);

                if (history.Count == 0 || history[^1] != inputField.text)
                    history.Add(inputField.text);

                if (history.Count > maxHistoryCount)
                {
                    history.RemoveAt(0);
                }

                inputField.text = "";
            }
            else
            {
                inputField.text = suggestions[suggestionIndex] + " ";
            }

#if !UNITY_ANDROID
            ForceSelection();
            StartCoroutine(UpdateCaretPositionAfterSingleFrame());
#endif
        }

        private IEnumerator UpdateCaretPositionAfterSingleFrame()
        {
            yield return null;
            inputField.ReleaseSelection();
            inputField.caretPosition = inputField.text.Length;
        }


        private void OnDeselectInput(string _)
        {
            if (!state) return;

#if !UNITY_ANDROID
            ForceSelection();
#endif
        }

        private void ForceSelection()
        {
            inputField.ActivateInputField();
        }

        private void OnInputFieldValueChanged(string arg0)
        {
            suggestionIndex = -1;
            historyIndex = -1;
            commandBeforeEnteringHistory = "";
            UpdateSuggestionSelection();

            console.FindSuggestions(inputField.text, ref suggestions);
            for (int i = 0; i < listedSuggestions.Count; i++)
            {
                if (i >= suggestions.Count)
                {
                    listedSuggestions[i].BindTo(null);
                }
                else
                {
                    listedSuggestions[i].BindTo(suggestions[i]);
                }
            }

            var currentCommand = console.FindClosestFittingCommand(inputField.text, out var usedSegments, out var allSegments);
            if (currentCommand == null)
            {
                if (suggestions.Count > 0)
                {
                    suggestionEcho.text = suggestions[0];
                }
                else
                {
                    suggestionEcho.text = "";
                }
            }
            else
            {
                int filledArgumentCount = allSegments - usedSegments;
                var argumentsToBeFilled = currentCommand.Arguments.Skip(filledArgumentCount);
                string argumentSuffix = string.Join(' ', argumentsToBeFilled.Where(a => a.IsSimple).Select(a => a.IsOptional ? $"[{a.Name}]" : $"<{a.Name}>"));
                suggestionEcho.text = $"{inputField.text.Trim()} <noparse>{argumentSuffix}";
            }
        }

        private void UpdateSuggestionSelection()
        {
            for (int i = 0; i < listedSuggestions.Count; i++)
            {
                listedSuggestions[i].IsSelected = i == suggestionIndex;
            }
        }

        private void SetCommandFromHistory()
        {
            var lastCommandBeforeEnteringHistory = commandBeforeEnteringHistory;
            var lastHistoryIndex = historyIndex;

            inputField.text = historyIndex == -1
                ? commandBeforeEnteringHistory
                : history[^(historyIndex + 1)];

            inputField.caretPosition = inputField.text.Length;

            historyIndex = lastHistoryIndex;
            commandBeforeEnteringHistory = lastCommandBeforeEnteringHistory;
        }

        [System.Serializable]
        private struct WrapperForStringList
        {
            public List<string> list;

            public WrapperForStringList(List<string> list)
            {
                this.list = list;
            }
        }
    }
}
