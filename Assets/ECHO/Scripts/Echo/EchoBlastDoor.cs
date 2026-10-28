using UnityEngine;

public sealed class EchoBlastDoor : MonoBehaviour
{
    [SerializeField] private float openHeight = 5f;
    [SerializeField] private float openingSpeed = 4f;

    private Vector3 closedPosition;
    private bool isOpen;

    public bool IsOpen { get { return isOpen; } }

    public void Initialize()
    {
        closedPosition = transform.localPosition;
        isOpen = false;
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
    }

    private void Update()
    {
        Vector3 target = closedPosition + (isOpen ? Vector3.up * openHeight : Vector3.zero);
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, openingSpeed * Time.deltaTime);
    }
}
