using UnityEngine;

namespace Wokarol
{
    [SelectionBase]
    public class Dice : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private int faceCount = 12;

        public Rigidbody Body => body;
    }
}
