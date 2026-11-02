using UnityEngine;
using UnityEngine.UI;

namespace NumberMerge.UI
{
    // Marks a transparent UI graphic (raycastTarget on) that swallows gameplay presses.
    // Gameplay input ignores any press whose UI raycast hits this object, so it can be used as a safe area.
    [RequireComponent(typeof(Graphic))]
    public sealed class UiInputBlocker : MonoBehaviour
    {
    }
}
