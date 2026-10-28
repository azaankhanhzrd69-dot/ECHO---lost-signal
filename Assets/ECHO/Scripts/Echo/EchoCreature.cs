using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public sealed class EchoCreature : MonoBehaviour
{
    private static readonly int[] RespawnRouteNodes = { 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13 };
    private EchoGame game;
    private readonly List<Renderer> creatureRenderers = new List<Renderer>();
    private readonly List<Material> runtimeMaterials = new List<Material>();
    private Animator animator;
    private CharacterController movementController;
    private PlayableGraph animationGraph;
    private AnimationMixerPlayable animationMixer;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable runPlayable;
    private AnimationClipPlayable ragePlayable;
    private AnimationClip idleClip;
    private AnimationClip runClip;
    private AnimationClip rageClip;
    private float rageWeight;
    private float rageTimer;
    private float nextRoarTime;
    private bool wasInvestigating;
    private AudioSource footstepSource;
    private AudioSource screamSource;
    private AudioClip footstepLoopClip;
    private AudioClip proximityRoarClip;
    private AudioClip deathRoarClip;
    private AudioClip[] singleFootsteps;
    private AudioClip warningClip;
    private float discreteStepTimer;
    private Vector3 investigationPoint;
    private Vector3 patrolPoint;
    private float investigationTime;
    private float patrolTimer;
    private float animationTime;
    private float chaseBlend;
    private int patrolIndex;
    private int lastRespawnNode = -1;
    private Vector3 respawnPosition = new Vector3(21f, 0f, 0f);
    private bool investigating;
    private bool revealed;
    private bool isVisible = true;
    private float revealUntil;
    public bool IsInvestigating { get { return investigating; } }
    public Vector3 HeadPosition
    {
        get
        {
            return headTransform != null ? headTransform.position : transform.position + Vector3.up * 2.3f;
        }
    }

    public void Initialize(
        EchoGame owner,
        GameObject modelPrefab,
        Material materialTemplate,
        AudioClip footstepLoopAudio,
        AudioClip proximityRoarAudio,
        AudioClip deathRoarAudio,
        AudioClip[] individualSteps,
        AudioClip warningAlert)
    {
        game = owner;
        footstepLoopClip = footstepLoopAudio;
        proximityRoarClip = proximityRoarAudio;
        deathRoarClip = deathRoarAudio;
        singleFootsteps = individualSteps;
        warningClip = warningAlert;
        BuildBody(modelPrefab, materialTemplate);
        patrolPoint = game.GetRoutePosition(4);
    }

    private Transform spineTransform;
    private Transform spine1Transform;
    private Transform spine2Transform;
    private Transform neckTransform;
    private Transform headTransform;

    private void BuildBody(GameObject modelPrefab, Material materialTemplate)
    {
        GameObject model = Instantiate(modelPrefab, transform, false);
        model.name = "Monster Mutant 7";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = new Vector3(2.1f, 1.58f, 2.1f);

        Transform[] allTransforms = model.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            string tName = allTransforms[i].name;
            if (tName == "Character1_Spine") spineTransform = allTransforms[i];
            else if (tName == "Character1_Spine1") spine1Transform = allTransforms[i];
            else if (tName == "Character1_Spine2") spine2Transform = allTransforms[i];
            else if (tName == "Character1_Neck") neckTransform = allTransforms[i];
            else if (tName == "Character1_Head") headTransform = allTransforms[i];
        }
        animator = model.GetComponent<Animator>();
        if (animator == null)
            throw new System.InvalidOperationException("The Monster Mutant 7 prefab is missing its Animator component.");
        animator.runtimeAnimatorController = null;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new System.InvalidOperationException("The Monster Mutant 7 prefab has no renderers.");
        creatureRenderers.AddRange(renderers);
        ApplyPinkTexturedMaterials(renderers, materialTemplate);
        LoadAnimationClips();
        CreateAnimationGraph();

        movementController = gameObject.AddComponent<CharacterController>();
        movementController.height = 2.45f;
        movementController.radius = 0.7f;
        movementController.center = new Vector3(0f, 1.25f, 0f);
        movementController.slopeLimit = 45f;
        movementController.stepOffset = 0.3f;
        movementController.skinWidth = 0.06f;
        movementController.minMoveDistance = 0f;

        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.spatialBlend = 1f;
        footstepSource.rolloffMode = AudioRolloffMode.Linear;
        footstepSource.minDistance = 2.5f;
        footstepSource.maxDistance = 35f;
        footstepSource.playOnAwake = false;
        footstepSource.dopplerLevel = 0f;
        if (footstepLoopClip != null)
        {
            footstepSource.clip = footstepLoopClip;
            footstepSource.loop = true;
        }

        screamSource = gameObject.AddComponent<AudioSource>();
        screamSource.spatialBlend = 1f;
        screamSource.rolloffMode = AudioRolloffMode.Linear;
        screamSource.minDistance = 3.5f;
        screamSource.maxDistance = 60f;
        screamSource.playOnAwake = false;
        screamSource.dopplerLevel = 0f;

        Light eyeGlow = gameObject.AddComponent<Light>();
        eyeGlow.type = LightType.Point;
        eyeGlow.color = new Color(1f, 0.04f, 0.015f);
        eyeGlow.range = 3.8f;
        eyeGlow.intensity = 1.6f;
        eyeGlow.shadows = LightShadows.None;
        SetVisible(false);
    }

    private void LoadAnimationClips()
    {
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>("MonsterAnimations");
        for (int i = 0; i < clips.Length; i++)
        {
            string cName = clips[i].name;
            if (idleClip == null && cName.IndexOf("idle1", System.StringComparison.OrdinalIgnoreCase) >= 0)
                idleClip = clips[i];
            if (runClip == null && cName.IndexOf("run1", System.StringComparison.OrdinalIgnoreCase) >= 0)
                runClip = clips[i];
            if (rageClip == null && (cName.IndexOf("rage", System.StringComparison.OrdinalIgnoreCase) >= 0 || cName.IndexOf("attack", System.StringComparison.OrdinalIgnoreCase) >= 0))
                rageClip = clips[i];
        }

        if (rageClip == null)
            rageClip = runClip;

        if (idleClip == null || runClip == null || idleClip.length <= 0f || runClip.length <= 0f)
            throw new System.InvalidOperationException(
                "The Monster Mutant 7 idle1 and run1 animation clips must be imported under Resources/MonsterAnimations with valid lengths.");
    }

    private void CreateAnimationGraph()
    {
        animationGraph = PlayableGraph.Create("Monster Mutant 7 animation");
        animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        animationMixer = AnimationMixerPlayable.Create(animationGraph, 3);
        idlePlayable = AnimationClipPlayable.Create(animationGraph, idleClip);
        runPlayable = AnimationClipPlayable.Create(animationGraph, runClip);
        ragePlayable = AnimationClipPlayable.Create(animationGraph, rageClip);
        idlePlayable.SetSpeed(0d);
        runPlayable.SetSpeed(0d);
        ragePlayable.SetSpeed(0d);
        animationGraph.Connect(idlePlayable, 0, animationMixer, 0);
        animationGraph.Connect(runPlayable, 0, animationMixer, 1);
        animationGraph.Connect(ragePlayable, 0, animationMixer, 2);
        animationMixer.SetInputWeight(0, 1f);
        animationMixer.SetInputWeight(1, 0f);
        animationMixer.SetInputWeight(2, 0f);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "Monster Mutant 7", animator);
        output.SetSourcePlayable(animationMixer);
        animationGraph.Play();
    }

    private void ApplyPinkTexturedMaterials(Renderer[] renderers, Material materialTemplate)
    {
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Material[] sourceMaterials = renderers[rendererIndex].sharedMaterials;
            Material[] convertedMaterials = new Material[sourceMaterials.Length];
            for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
            {
                Material source = sourceMaterials[materialIndex];
                if (source == null)
                    throw new System.InvalidOperationException("The Monster Mutant 7 skin contains a missing material.");

                Material converted = new Material(materialTemplate);
                CopyTextureProperty(source, converted, "_MainTex", "_BaseMap");
                CopyTextureProperty(source, converted, "_BumpMap", "_BumpMap");
                CopyTextureProperty(source, converted, "_MetallicGlossMap", "_MetallicGlossMap");
                CopyTextureProperty(source, converted, "_OcclusionMap", "_OcclusionMap");
                CopyTextureProperty(source, converted, "_EmissionMap", "_EmissionMap");

                Color sourceColor = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                Color pinkTint = new Color(1f, 0.48f, 0.68f);
                Color tintedColor = new Color(sourceColor.r * pinkTint.r, sourceColor.g * pinkTint.g,
                    sourceColor.b * pinkTint.b, sourceColor.a);
                if (converted.HasProperty("_BaseColor"))
                    converted.SetColor("_BaseColor", tintedColor);
                else
                    converted.color = tintedColor;

                if (source.HasProperty("_Metallic") && converted.HasProperty("_Metallic"))
                    converted.SetFloat("_Metallic", source.GetFloat("_Metallic"));
                if (source.HasProperty("_Glossiness") && converted.HasProperty("_Smoothness"))
                    converted.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
                if (source.HasProperty("_OcclusionStrength") && converted.HasProperty("_OcclusionStrength"))
                    converted.SetFloat("_OcclusionStrength", source.GetFloat("_OcclusionStrength"));
                if (source.HasProperty("_EmissionColor") && converted.HasProperty("_EmissionColor"))
                {
                    converted.EnableKeyword("_EMISSION");
                    converted.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
                }

                if (converted.GetTexture("_BumpMap") != null)
                    converted.EnableKeyword("_NORMALMAP");
                if (converted.GetTexture("_MetallicGlossMap") != null)
                    converted.EnableKeyword("_METALLICSPECGLOSSMAP");
                runtimeMaterials.Add(converted);
                convertedMaterials[materialIndex] = converted;
            }

            renderers[rendererIndex].sharedMaterials = convertedMaterials;
        }
    }

    private static void CopyTextureProperty(Material source, Material destination, string sourceProperty, string destinationProperty)
    {
        if (!source.HasProperty(sourceProperty) || !destination.HasProperty(destinationProperty))
            return;

        Texture texture = source.GetTexture(sourceProperty);
        if (texture == null)
            return;

        destination.SetTexture(destinationProperty, texture);
        destination.SetTextureScale(destinationProperty, source.GetTextureScale(sourceProperty));
        destination.SetTextureOffset(destinationProperty, source.GetTextureOffset(sourceProperty));
    }

    private void Update()
    {
        if (game == null || !game.CanEmitPulse)
            return;

        float distance = Vector3.Distance(transform.localPosition, game.PlayerPosition);
        float detectRange = game.IsCrouching ? 8.5f : 20f;
        if (game.FlashlightOn)
            detectRange += 8f;

        if (distance < detectRange)
        {
            investigationPoint = game.PlayerPosition;
            investigating = true;
            investigationTime = 18f;
        }

        if (investigating)
        {
            if (distance < 28f)
            {
                investigationPoint = game.PlayerPosition;
                investigationTime = 16f;
            }

            investigationTime -= Time.deltaTime;
            if (investigationTime <= 0f && distance > 10f)
            {
                investigating = false;
                patrolTimer = 0f;
            }
        }

        if (!investigating)
        {
            patrolTimer -= Time.deltaTime;
            if (patrolTimer <= 0f || Vector3.Distance(transform.localPosition, patrolPoint) < 1.5f)
            {
                patrolIndex = (patrolIndex + 1) % 4;
                patrolPoint = game.GetRoutePosition(patrolIndex == 0 ? 3 : patrolIndex == 1 ? 5 : patrolIndex == 2 ? 2 : 4);
                patrolTimer = 4f;
            }
        }

        Vector3 target = investigating ? investigationPoint : patrolPoint;
        if (!investigating || distance > 8f)
        {
            int nextNode = game.GetNextRouteNode(transform.localPosition, target);
            if (nextNode >= 0 && Vector3.Distance(transform.localPosition, target) > 2.2f)
                target = game.GetRoutePosition(nextNode);
        }
        target.y = transform.localPosition.y;

        float speed = investigating
            ? (distance < 6f ? 7.5f : 6.85f)
            : 2.35f;
        Vector3 oldPosition = transform.localPosition;
        Vector3 movement = target - oldPosition;
        movement.y = 0f;
        if (movement.sqrMagnitude > 0.001f)
        {
            movement = movement.normalized * speed * Time.deltaTime;
            movementController.Move(transform.parent.TransformDirection(movement));
        }

        Vector3 direction = transform.localPosition - oldPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 12f);

        bool isMoving = movement.sqrMagnitude > 0.0005f;
        if (footstepLoopClip != null && footstepSource != null)
        {
            if (isMoving)
            {
                if (!footstepSource.isPlaying)
                    footstepSource.Play();

                float targetVol = investigating ? 0.95f : 0.65f;
                footstepSource.volume = Mathf.MoveTowards(footstepSource.volume, targetVol, Time.deltaTime * 3.5f);
                footstepSource.pitch = investigating ? 1.15f : 0.8f;
            }
            else
            {
                if (footstepSource.isPlaying)
                {
                    footstepSource.volume = Mathf.MoveTowards(footstepSource.volume, 0f, Time.deltaTime * 5f);
                    if (footstepSource.volume <= 0.01f)
                        footstepSource.Pause();
                }
            }
        }
        else if (singleFootsteps != null && singleFootsteps.Length > 0 && footstepSource != null)
        {
            if (isMoving)
            {
                discreteStepTimer -= Time.deltaTime;
                if (discreteStepTimer <= 0f)
                {
                    discreteStepTimer = investigating ? 0.34f : 0.62f;
                    AudioClip clip = singleFootsteps[Random.Range(0, singleFootsteps.Length)];
                    if (clip != null)
                    {
                        footstepSource.pitch = Random.Range(0.42f, 0.52f);
                        footstepSource.PlayOneShot(clip, investigating ? 0.95f : 0.7f);
                    }
                }
            }
            else
            {
                discreteStepTimer = 0.1f;
            }
        }

        if (!wasInvestigating && investigating)
            PlayRoar(0.9f);
        else if (investigating && distance < 14f)
            PlayRoar(0.9f);
        wasInvestigating = investigating;

        if (revealed && Time.time > revealUntil)
            revealed = false;

        SetVisible(revealed || investigating || distance < 9f);
        SetAnimation(investigating);
    }

    private void LateUpdate()
    {
        // Wide, imposing, hunched predator posture that stays beneath the ceiling
        if (spineTransform != null)
        {
            spineTransform.localScale = new Vector3(1.15f, 1.05f, 1.15f);
            spineTransform.localRotation = spineTransform.localRotation * Quaternion.Euler(14f, 0f, 0f);
        }
        if (spine1Transform != null)
        {
            spine1Transform.localScale = new Vector3(1.12f, 1.03f, 1.12f);
            spine1Transform.localRotation = spine1Transform.localRotation * Quaternion.Euler(10f, 0f, 0f);
        }
        if (spine2Transform != null)
        {
            spine2Transform.localScale = new Vector3(1.1f, 1.02f, 1.1f);
        }

        // Twisted, predatory head angle
        if (neckTransform != null)
            neckTransform.localRotation = neckTransform.localRotation * Quaternion.Euler(18f, 32f, -18f);
        if (headTransform != null)
            headTransform.localRotation = headTransform.localRotation * Quaternion.Euler(-8f, 14f, 10f);
    }

    private void SetAnimation(bool chasing)
    {
        if (!animationGraph.IsValid())
            return;

        animationTime += Time.deltaTime;
        idlePlayable.SetTime(animationTime % idleClip.length);
        runPlayable.SetTime(animationTime * 1.15f % runClip.length);
        if (rageClip != null && rageClip.length > 0f)
            ragePlayable.SetTime((animationTime * 1.3f) % rageClip.length);

        if (rageTimer > 0f)
        {
            rageTimer -= Time.deltaTime;
            rageWeight = Mathf.MoveTowards(rageWeight, 1f, Time.deltaTime * 6f);
        }
        else
        {
            rageWeight = Mathf.MoveTowards(rageWeight, 0f, Time.deltaTime * 3f);
        }

        chaseBlend = Mathf.MoveTowards(chaseBlend, chasing ? 1f : 0f, Time.deltaTime * 3.5f);
        float nonRage = 1f - rageWeight;
        animationMixer.SetInputWeight(0, (1f - chaseBlend) * nonRage);
        animationMixer.SetInputWeight(1, chaseBlend * nonRage);
        animationMixer.SetInputWeight(2, rageWeight);
    }

    public void HearPulse(Vector3 source)
    {
        investigationPoint = source;
        investigating = true;
        investigationTime = 16f;
        PlayRoar(0.95f);
    }

    public void PlayRoar(float volume = 0.9f)
    {
        if (Time.time < nextRoarTime)
            return;

        nextRoarTime = Time.time + 5.5f;
        rageTimer = 1.6f;

        if (screamSource != null)
        {
            screamSource.spatialBlend = 1.0f;
            screamSource.pitch = Random.Range(0.92f, 1.06f);
            if (proximityRoarClip != null)
                screamSource.PlayOneShot(proximityRoarClip, volume);
            else if (deathRoarClip != null)
                screamSource.PlayOneShot(deathRoarClip, volume);
        }
    }

    public void TriggerJumpscareScream()
    {
        rageTimer = 2.8f;
        rageWeight = 1.0f;
        if (screamSource != null)
        {
            screamSource.spatialBlend = 0.05f;
            screamSource.pitch = 1.0f;
            if (deathRoarClip != null)
                screamSource.PlayOneShot(deathRoarClip, 1.0f);
            else if (proximityRoarClip != null)
                screamSource.PlayOneShot(proximityRoarClip, 1.0f);
        }
    }

    public void Reveal(float duration)
    {
        revealUntil = Time.time + duration;
        revealed = true;
        SetVisible(true);
    }

    public void SetVisible(bool visible)
    {
        if (isVisible == visible)
            return;

        isVisible = visible;
        for (int i = 0; i < creatureRenderers.Count; i++)
            creatureRenderers[i].enabled = visible;
    }

    public void ResetCreature()
    {
        if (footstepSource != null && footstepSource.isPlaying)
            footstepSource.Stop();
        if (screamSource != null && screamSource.isPlaying)
            screamSource.Stop();

        SetCreaturePosition(respawnPosition);
        transform.rotation = Quaternion.identity;
        investigationTime = 0f;
        investigating = false;
        wasInvestigating = false;
        patrolTimer = 0f;
        patrolIndex = 0;
        patrolPoint = game.GetRoutePosition(4);
        revealed = false;
        SetVisible(false);
        chaseBlend = 0f;
        rageTimer = 0f;
        rageWeight = 0f;
        SetAnimation(false);
    }

    public void PrepareRespawnAfterDeath(Vector3 deathPosition)
    {
        int selectedNode = -1;
        List<int> candidates = new List<int>(RespawnRouteNodes.Length);
        for (int i = 0; i < RespawnRouteNodes.Length; i++)
        {
            int node = RespawnRouteNodes[i];
            Vector3 candidate = game.GetRoutePosition(node);
            if (node != lastRespawnNode && Vector3.Distance(candidate, deathPosition) >= 14f)
                candidates.Add(node);
        }

        if (candidates.Count == 0)
        {
            for (int i = 0; i < RespawnRouteNodes.Length; i++)
            {
                int node = RespawnRouteNodes[i];
                if (node != lastRespawnNode)
                    candidates.Add(node);
            }
        }

        if (candidates.Count == 0)
            throw new System.InvalidOperationException("No alternate monster respawn points are available.");

        selectedNode = candidates[Random.Range(0, candidates.Count)];
        lastRespawnNode = selectedNode;
        respawnPosition = game.GetRoutePosition(selectedNode);
        SetCreaturePosition(respawnPosition);
        investigationTime = 0f;
        investigating = false;
        patrolTimer = 0f;
        patrolIndex = 0;
        patrolPoint = game.GetRoutePosition(4);
        revealed = false;
        SetVisible(false);
    }

    public void SetCreaturePosition(Vector3 position)
    {
        if (movementController != null)
            movementController.enabled = false;
        transform.localPosition = position;
        if (movementController != null)
            movementController.enabled = true;
    }

    private void OnDestroy()
    {
        if (animationGraph.IsValid())
            animationGraph.Destroy();

        for (int i = 0; i < runtimeMaterials.Count; i++)
            Destroy(runtimeMaterials[i]);
    }
}
