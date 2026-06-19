using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ShadowExposureDamage))]
public class PlayerAudioFeedback : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] private float footstepInterval = 0.46f;
    [SerializeField] private float minimumMoveSpeed = 0.35f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.24f;

    [Header("Light Warning")]
    [SerializeField, Range(0f, 1f)] private float warningVolume = 0.24f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private ShadowExposureDamage exposure;
    private AudioSource footstepSource;
    private AudioSource warningSource;
    private AudioClip[] footstepClips;
    private AudioClip warningClip;
    private float stepTimer;
    private int nextStepIndex;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        exposure = GetComponent<ShadowExposureDamage>();

        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = 0f;
        footstepSource.volume = footstepVolume;

        warningSource = gameObject.AddComponent<AudioSource>();
        warningSource.playOnAwake = false;
        warningSource.loop = true;
        warningSource.spatialBlend = 0f;
        warningSource.volume = warningVolume;

        footstepClips = new[] { CreateFootstepClip("Footstep A", 127), CreateFootstepClip("Footstep B", 389) };
        warningClip = CreateWarningClip();
        warningSource.clip = warningClip;
    }

    private void Update()
    {
        UpdateFootsteps();
        UpdateWarning();
    }

    private void OnDestroy()
    {
        if (footstepClips != null)
        {
            for (int i = 0; i < footstepClips.Length; i++)
            {
                if (footstepClips[i] != null) Destroy(footstepClips[i]);
            }
        }

        if (warningClip != null) Destroy(warningClip);
    }

    private void UpdateFootsteps()
    {
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;
        bool walking = horizontalVelocity.magnitude >= minimumMoveSpeed && IsGrounded();

        if (!walking)
        {
            stepTimer = footstepInterval * 0.55f;
            return;
        }

        stepTimer += Time.deltaTime;
        if (stepTimer < footstepInterval)
        {
            return;
        }

        stepTimer = 0f;
        footstepSource.pitch = nextStepIndex == 0 ? 0.96f : 1.04f;
        footstepSource.PlayOneShot(footstepClips[nextStepIndex]);
        nextStepIndex = (nextStepIndex + 1) % footstepClips.Length;
    }

    private void UpdateWarning()
    {
        bool shouldWarn = exposure != null && !exposure.IsInShadow;
        if (shouldWarn && !warningSource.isPlaying)
        {
            warningSource.Play();
        }
        else if (!shouldWarn && warningSource.isPlaying)
        {
            warningSource.Stop();
        }
    }

    private bool IsGrounded()
    {
        if (capsule == null)
        {
            return true;
        }

        Bounds bounds = capsule.bounds;
        float rayDistance = bounds.extents.y + 0.18f;
        return Physics.Raycast(bounds.center, Vector3.down, rayDistance, ~0, QueryTriggerInteraction.Ignore);
    }

    private static AudioClip CreateFootstepClip(string clipName, int seed)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.RoundToInt(sampleRate * 0.22f);
        float[] samples = new float[sampleCount];
        uint state = (uint)seed;
        float filteredNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            state = state * 1664525u + 1013904223u;
            float noise = ((state >> 8) / 8388607.5f) - 1f;
            filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.055f);

            float heelEnvelope = Mathf.Exp(-19f * time);
            float heelTone = Mathf.Sin(2f * Mathf.PI * 68f * time) * heelEnvelope;

            float soleTime = Mathf.Max(0f, time - 0.055f);
            float soleEnvelope = time >= 0.055f ? Mathf.Exp(-25f * soleTime) : 0f;
            float soleTone = Mathf.Sin(2f * Mathf.PI * 112f * soleTime) * soleEnvelope;

            float softTexture = filteredNoise * (heelEnvelope * 0.08f + soleEnvelope * 0.05f);
            float fadeIn = Mathf.Clamp01(time * 160f);
            samples[i] = (heelTone * 0.38f + soleTone * 0.2f + softTexture) * fadeIn;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateWarningClip()
    {
        const int sampleRate = 22050;
        const float duration = 1.15f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            samples[i] = CreateWarningPulse(time, 0.05f, 0.19f) + CreateWarningPulse(time, 0.33f, 0.19f);
        }

        AudioClip clip = AudioClip.Create("Light Warning", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float CreateWarningPulse(float time, float start, float length)
    {
        float localTime = time - start;
        if (localTime < 0f || localTime > length)
        {
            return 0f;
        }

        float envelope = Mathf.Sin(Mathf.PI * localTime / length);
        return Mathf.Sin(2f * Mathf.PI * 760f * localTime) * envelope * 0.42f;
    }
}
