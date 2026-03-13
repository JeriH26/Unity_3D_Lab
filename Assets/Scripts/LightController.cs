using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dynamic light manager that supports flickering, color cycling,
/// and zone-based light activation.
/// </summary>
public class LightController : MonoBehaviour
{
    [System.Serializable]
    public class ManagedLight
    {
        public Light light;
        public bool flicker;
        public float flickerSpeed = 10f;
        public float flickerMinIntensity = 0.5f;
        public float flickerMaxIntensity = 1.5f;
        public bool cycleColor;
        public Gradient colorCycle;
        public float colorCycleDuration = 3f;
        [HideInInspector] public float colorTimer;
        [HideInInspector] public float baseIntensity;
    }

    [Header("Lights")]
    [SerializeField] private List<ManagedLight> managedLights = new List<ManagedLight>();

    [Header("Day/Night Cycle")]
    [SerializeField] private bool enableDayNightCycle;
    [SerializeField] private Light sunLight;
    [SerializeField] private float dayDuration = 120f;
    [SerializeField] private Gradient skyColorGradient;
    [SerializeField] private AnimationCurve sunIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Zone Lighting")]
    [SerializeField] private bool useZoneLighting;
    [SerializeField] private float zoneActivationRadius = 15f;
    [SerializeField] private Transform player;

    private float _dayTimer;

    private void Start()
    {
        foreach (ManagedLight ml in managedLights)
        {
            if (ml.light != null)
                ml.baseIntensity = ml.light.intensity;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void Update()
    {
        foreach (ManagedLight ml in managedLights)
        {
            if (ml.light == null) continue;

            if (useZoneLighting && player != null)
            {
                bool inZone = Vector3.Distance(player.position, ml.light.transform.position) <= zoneActivationRadius;
                ml.light.enabled = inZone;
                if (!inZone) continue;
            }

            if (ml.flicker)
                UpdateFlicker(ml);

            if (ml.cycleColor)
                UpdateColorCycle(ml);
        }

        if (enableDayNightCycle && sunLight != null)
            UpdateDayNightCycle();
    }

    private void UpdateFlicker(ManagedLight ml)
    {
        float noise = Mathf.PerlinNoise(Time.time * ml.flickerSpeed, ml.light.GetInstanceID() * 0.1f);
        ml.light.intensity = Mathf.Lerp(ml.flickerMinIntensity, ml.flickerMaxIntensity, noise);
    }

    private void UpdateColorCycle(ManagedLight ml)
    {
        ml.colorTimer += Time.deltaTime / ml.colorCycleDuration;
        if (ml.colorTimer > 1f) ml.colorTimer -= 1f;
        ml.light.color = ml.colorCycle.Evaluate(ml.colorTimer);
    }

    private void UpdateDayNightCycle()
    {
        _dayTimer += Time.deltaTime / dayDuration;
        if (_dayTimer > 1f) _dayTimer -= 1f;

        float sunAngle = _dayTimer * 360f - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
        sunLight.intensity = sunIntensityCurve.Evaluate(Mathf.Sin(_dayTimer * Mathf.PI));

        if (skyColorGradient != null)
            RenderSettings.ambientLight = skyColorGradient.Evaluate(_dayTimer);

        // Toggle shadows: only cast shadows during "daytime"
        sunLight.shadows = (_dayTimer > 0.25f && _dayTimer < 0.75f)
            ? LightShadows.Soft
            : LightShadows.None;
    }

    /// <summary>Adds a light to the managed list at runtime.</summary>
    public void RegisterLight(Light light, bool flicker = false, bool cycleColor = false)
    {
        managedLights.Add(new ManagedLight
        {
            light = light,
            flicker = flicker,
            cycleColor = cycleColor,
            baseIntensity = light.intensity
        });
    }

    /// <summary>Removes a light from the managed list.</summary>
    public void UnregisterLight(Light light)
    {
        managedLights.RemoveAll(ml => ml.light == light);
    }

    private void OnDrawGizmosSelected()
    {
        if (!useZoneLighting) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        foreach (ManagedLight ml in managedLights)
        {
            if (ml.light != null)
                Gizmos.DrawWireSphere(ml.light.transform.position, zoneActivationRadius);
        }
    }
}
