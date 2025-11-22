using System.Collections.Generic;
using UnityEngine;

namespace Wokarol.Common
{
    public class InputBlocker : MonoBehaviour
    {
        private readonly HashSet<object> blocker = new();

        public bool IsBlocked => blocker.Count > 0;
        public int Count => blocker.Count;

        public void Block(object caller)
        {
            blocker.Add(caller);
        }

        public void Unlock(object caller)
        {
            blocker.Remove(caller);
        }

        public bool TheOnlyBlockerIs(object caller)
        {
            return blocker.Count == 1 && blocker.Contains(caller);
        }
    }
}
