using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class EchoPulse : MonoBehaviour
{
    [Header("Pulse settings")]
    public float maxRadius = 21f;
    public float pulseDuration = 1.35f;
    public float cooldown = 2.4f;
    public Color pulseColor = new Color(0.12f, 0.9f, 1f, 1f);
    public float waveThickness = 0.11f;

    private Camera sourceCamera;
    private float nextPulseTime;
    private Light pulseLight;
    private AudioSource pulseAudio;
    private Material waveMaterial;
    private AudioClip pulseClip;

    public void Bind(Camera camera)
    {
        sourceCamera = camera;
        GameObject lightObject = new GameObject("Sonar flash");
        lightObject.transform.SetParent(camera.transform, false);
        pulseLight = lightObject.AddComponent<Light>();
        pulseLight.type = LightType.Point;
        pulseLight.color = pulseColor;
        pulseLight.range = maxRadius * 0.48f;
        pulseLight.intensity = 0f;
        pulseLight.shadows = LightShadows.None;
        pulseAudio = camera.gameObject.AddComponent<AudioSource>();
        pulseAudio.playOnAwake = false;
        pulseAudio.spatialBlend = 0f;
        pulseAudio.volume = 0.6f;
        pulseClip = CreatePulseSound();

        Material sourceMaterial = Resources.Load<Material>("EchoLit");
        if (sourceMaterial == null || sourceMaterial.shader == null)
            throw new System.InvalidOperationException("The EchoLit material and its URP/Lit shader are required.");
        waveMaterial = new Material(sourceMaterial);
        waveMaterial.color = pulseColor;
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.qKey.wasPressedThisFrame ||
            Time.time < nextPulseTime || EchoGame.Instance == null || !EchoGame.Instance.CanEmitPulse)
            return;

        nextPulseTime = Time.time + cooldown;
        EchoGame.Instance.OnPulse();
        pulseAudio.PlayOneShot(pulseClip);
        StartCoroutine(EmitPulse());
    }

    private static AudioClip CreatePulseSound()
    {
        const int sampleRate = 22050;
        const int sampleCount = 11025;
        float[] samples = new float[sampleCount];
        for (int i = 0; i < samples.Length; i++)
        {
            float time = i / (float)sampleRate;
            float envelope = Mathf.Exp(-time * 7f);
            float frequency = Mathf.Lerp(118f, 62f, time / 0.5f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * 0.65f;
        }

        AudioClip clip = AudioClip.Create("Sonar ping", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private IEnumerator EmitPulse()
    {
        GameObject wave = new GameObject("Expanding sonar ring");
        wave.transform.position = sourceCamera.transform.position + sourceCamera.transform.forward * 0.55f;
        wave.transform.rotation = sourceCamera.transform.rotation;
        LineRenderer line = wave.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 96;
        line.startWidth = waveThickness;
        line.endWidth = waveThickness;
        line.numCapVertices = 4;
        line.sharedMaterial = waveMaterial;
        line.startColor = pulseColor;
        line.endColor = new Color(pulseColor.r, pulseColor.g, pulseColor.b, 0.1f);

        EchoGame.Instance.RevealStation(transform.position, maxRadius, 2.8f);

        EchoCreature creature = FindAnyObjectByType<EchoCreature>();
        if (creature != null && Vector3.Distance(transform.position, creature.transform.position) <= maxRadius)
            creature.Reveal(2.4f);

        float elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            float progress = elapsed / pulseDuration;
            float radius = Mathf.Lerp(0.2f, maxRadius, progress);
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }

            pulseLight.intensity = Mathf.Lerp(7f, 0f, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        pulseLight.intensity = 0f;
        Destroy(wave);
    }

    private void OnDestroy()
    {
        if (waveMaterial != null)
            Destroy(waveMaterial);
        if (pulseClip != null)
            Destroy(pulseClip);
    }
}
