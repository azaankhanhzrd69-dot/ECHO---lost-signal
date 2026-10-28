using Echo.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class EchoPlayerMotor : MonoBehaviour
{
    private EchoGame game;
    private CharacterController controller;
    private Light flashlight;
    private Camera playerCamera;
    private float verticalSpeed;
    private float pitch;
    private float yaw = 90f;
    private float cameraBob;
    private bool controlsEnabled;
    private float originalHeight;
    private AudioSource playerAudioSource;
    private AudioClip[] playerFootstepClips;
    private float playerFootstepTimer;

    public void SetFootstepAudio(AudioClip[] clips)
    {
        playerFootstepClips = clips;
        if (playerAudioSource == null)
        {
            playerAudioSource = gameObject.AddComponent<AudioSource>();
            playerAudioSource.spatialBlend = 0f;
            playerAudioSource.playOnAwake = false;
        }
    }

    public void Initialize(EchoGame owner, CharacterController character)
    {
        game = owner;
        controller = character;
        originalHeight = character.height;
        playerCamera = GetComponentInChildren<Camera>();
        controlsEnabled = false;
    }

    public void SetFlashlight(Light light)
    {
        flashlight = light;
    }

    public void SetFlashlightActive(bool active)
    {
        if (flashlight != null)
            flashlight.enabled = active;
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
    }

    public void SetCamera(Camera camera)
    {
        playerCamera = camera;
    }

    public void ResetMotor()
    {
        verticalSpeed = 0f;
        pitch = 0f;
        yaw = 90f;
        controlsEnabled = true;
        controller.height = originalHeight;
        controller.center = new Vector3(0f, 0.9f, 0f);
    }

    private void Update()
    {
        if (!controlsEnabled || EchoEvents.InputLocked || Keyboard.current == null || Mouse.current == null)
            return;

        Vector2 look = Mouse.current.delta.ReadValue() * 0.08f;
        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, -82f, 82f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        Vector2 input = Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        bool crouching = game.IsCrouching;
        controller.height = Mathf.Lerp(controller.height, crouching ? 1.05f : originalHeight, Time.deltaTime * 10f);
        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
        bool sprinting = keyboard.leftShiftKey.isPressed && !crouching && input.y > 0.1f;
        float speed = crouching ? 2.25f : sprinting ? 6.3f : 4.25f;
        Vector3 movement = transform.right * input.x + transform.forward * input.y;

        if (controller.isGrounded)
        {
            if (verticalSpeed < 0f)
                verticalSpeed = -1.5f;
            if (keyboard.spaceKey.wasPressedThisFrame)
                verticalSpeed = 4.8f;
        }

        verticalSpeed -= 18f * Time.deltaTime;
        movement *= speed;
        movement.y = verticalSpeed;
        controller.Move(movement * Time.deltaTime);

        float movementAmount = input.magnitude;
        if (movementAmount > 0.1f && controller.isGrounded && playerFootstepClips != null && playerFootstepClips.Length > 0)
        {
            playerFootstepTimer -= Time.deltaTime;
            if (playerFootstepTimer <= 0f)
            {
                playerFootstepTimer = sprinting ? 0.32f : crouching ? 0.62f : 0.44f;
                AudioClip clip = playerFootstepClips[Random.Range(0, playerFootstepClips.Length)];
                if (clip != null && playerAudioSource != null)
                {
                    playerAudioSource.pitch = Random.Range(0.92f, 1.08f);
                    playerAudioSource.PlayOneShot(clip, crouching ? 0.16f : sprinting ? 0.48f : 0.32f);
                }
            }
        }

        cameraBob += Time.deltaTime * (sprinting ? 12f : 8f) * movementAmount;
        Vector3 cameraPosition = new Vector3(0f, controller.height * 0.5f + 0.65f, 0f);
        if (movementAmount > 0.05f && controller.isGrounded)
            cameraPosition.y += Mathf.Sin(cameraBob) * 0.025f;
        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, cameraPosition, Time.deltaTime * 14f);
    }
}

