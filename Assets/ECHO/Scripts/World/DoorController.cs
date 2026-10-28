using System.Collections;
using UnityEngine;

namespace Echo.World
{
    /// <summary>
    /// Door that opens/closes and locks/unlocks via public methods.
    /// UnityEvents can wire these methods with no cross-lane references.
    /// </summary>
    public class DoorController : MonoBehaviour
    {
        [SerializeField] private Transform door;
        [SerializeField] private Vector3 openOffset;
        [SerializeField] private float openDuration = 1f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openClip;
        [SerializeField] private AudioClip closeClip;
        [SerializeField] private AudioClip lockedClip;

        private bool isOpen;
        private bool isLocked;
        private bool isMoving;

        public bool IsLocked => isLocked;
        public bool IsOpen => isOpen;

        public void Open()
        {
            if (isLocked) return;
            if (isMoving) return;
            isOpen = true;
            isMoving = true;
            StartCoroutine(MoveRoutine(true));
        }

        public void Close()
        {
            isOpen = false;
            isMoving = true;
            StartCoroutine(MoveRoutine(false));
        }

        public void Lock()
        {
            isLocked = true;
        }

        public void Unlock()
        {
            isLocked = false;
        }

        private IEnumerator MoveRoutine(bool open)
        {
            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                if (!isMoving) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                door.localPosition = Vector3.Lerp(door.localPosition, open ? openOffset : Vector3.zero, t);
                yield return null;
            }

            door.localPosition = open ? openOffset : Vector3.zero;
            isMoving = false;
        }
    }
}