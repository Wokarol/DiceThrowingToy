using UnityEngine;
using UnityEngine.UI;

namespace Wokarol.GodConsole.View
{
    public class ListedSuggestionElement : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text label;
        [SerializeField] private Image image;
        [SerializeField] private Color selectionColor;

        private bool isSelected;
        private Color baseColor;

        public bool IsSelected 
        {
            get => isSelected;
            set
            {
                if (isSelected == value) return;

                isSelected = value;
                UpdateSelectionVisuals();
            }
        }

        private void Awake()
        {
            baseColor = image.color;
        }

        public void BindTo(string path)
        {
            if (path == null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                label.text = path;
            }
        }

        private void UpdateSelectionVisuals()
        {
            image.color = isSelected
                ? selectionColor
                : baseColor;
        }
    }
}
