using UnityEngine;

public sealed class EchoInteractable : MonoBehaviour
{
    public string Kind { get; private set; }
    public string Label { get; private set; }
    public int Index { get; private set; }

    public void Initialize(string kind, int index, string label)
    {
        Kind = kind;
        Index = index;
        Label = label;
    }
}

