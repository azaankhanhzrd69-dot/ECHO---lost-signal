using System.Collections.Generic;
using UnityEngine;

public sealed class EchoPulseReveal : MonoBehaviour
{
    private readonly List<Renderer> renderers = new List<Renderer>();
    private float visibleUntil;
    private bool prepared;

    public void Prepare()
    {
        renderers.Clear();
        renderers.AddRange(GetComponentsInChildren<Renderer>(true));
        for (int i = 0; i < renderers.Count; i++)
            renderers[i].enabled = false;
        prepared = true;
    }

    public void Reveal(float duration)
    {
        if (!prepared)
            Prepare();

        for (int i = 0; i < renderers.Count; i++)
            renderers[i].enabled = true;
        visibleUntil = Mathf.Max(visibleUntil, Time.time + duration);
    }

    private void Update()
    {
        if (prepared && renderers.Count > 0 && Time.time >= visibleUntil)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = false;
            }
        }
    }
}
