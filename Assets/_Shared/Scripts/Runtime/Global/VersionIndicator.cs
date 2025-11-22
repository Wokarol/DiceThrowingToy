using UnityEngine;

namespace Wokarol.Common
{
    public class VersionIndicator : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text label;
        [Tooltip("Available placeholders: \n{G} - Game name\n{C} - Company name\n{V} - Version")]
        [SerializeField, TextArea(3, 5)] private string format = "{G} by {C}, v{V}";

        private void Start()
        {
            label.text = format
                .Replace("{G}", Application.productName)
                .Replace("{C}", Application.companyName)
                .Replace("{V}", Application.version);
        }
    }
}
