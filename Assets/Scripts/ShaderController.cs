using UnityEngine;

/// <summary>
/// Runtime shader property controller.
/// Allows animating and adjusting shader properties from code or the Inspector.
/// Attach to any GameObject with a Renderer component.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ShaderController : MonoBehaviour
{
    [Header("Color Properties")]
    [SerializeField] private bool animateColor;
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private float colorCycleDuration = 3f;
    [SerializeField] private string colorPropertyName = "_BaseColor";

    [Header("Float Properties")]
    [SerializeField] private bool animateFloat;
    [SerializeField] private string floatPropertyName = "_DissolveAmount";
    [SerializeField] private float floatMin;
    [SerializeField] private float floatMax = 1f;
    [SerializeField] private float floatCycleDuration = 2f;
    [SerializeField] private AnimationCurve floatCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Emission")]
    [SerializeField] private bool animateEmission;
    [SerializeField] private Color emissionColor = Color.white;
    [SerializeField] private float emissionIntensityMin;
    [SerializeField] private float emissionIntensityMax = 3f;
    [SerializeField] private float emissionCycleDuration = 1.5f;
    [SerializeField] private string emissionPropertyName = "_EmissionColor";

    [Header("Texture Offset/Scroll")]
    [SerializeField] private bool scrollTexture;
    [SerializeField] private string texturePropertyName = "_BaseMap";
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.1f, 0f);

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;
    private float _colorTimer;
    private float _floatTimer;
    private float _emissionTimer;
    private Vector2 _textureOffset;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        _renderer.GetPropertyBlock(_propertyBlock);

        if (animateColor)
            UpdateColorAnimation();

        if (animateFloat)
            UpdateFloatAnimation();

        if (animateEmission)
            UpdateEmissionAnimation();

        if (scrollTexture)
            UpdateTextureScroll();

        _renderer.SetPropertyBlock(_propertyBlock);
    }

    private void UpdateColorAnimation()
    {
        _colorTimer += Time.deltaTime / colorCycleDuration;
        if (_colorTimer > 1f) _colorTimer -= 1f;
        Color c = colorGradient.Evaluate(_colorTimer);
        _propertyBlock.SetColor(colorPropertyName, c);
    }

    private void UpdateFloatAnimation()
    {
        _floatTimer += Time.deltaTime / floatCycleDuration;
        if (_floatTimer > 1f) _floatTimer -= 1f;
        float ping = Mathf.PingPong(_floatTimer * 2f, 1f);
        float value = Mathf.Lerp(floatMin, floatMax, floatCurve.Evaluate(ping));
        _propertyBlock.SetFloat(floatPropertyName, value);
    }

    private void UpdateEmissionAnimation()
    {
        _emissionTimer += Time.deltaTime / emissionCycleDuration;
        if (_emissionTimer > 1f) _emissionTimer -= 1f;
        float intensity = Mathf.Lerp(emissionIntensityMin, emissionIntensityMax,
            Mathf.PingPong(_emissionTimer * 2f, 1f));
        _propertyBlock.SetColor(emissionPropertyName, emissionColor * intensity);
    }

    private void UpdateTextureScroll()
    {
        _textureOffset += scrollSpeed * Time.deltaTime;
        _propertyBlock.SetVector(texturePropertyName + "_ST",
            new Vector4(1f, 1f, _textureOffset.x, _textureOffset.y));
    }

    /// <summary>Sets a float shader property directly.</summary>
    public void SetFloat(string propertyName, float value)
    {
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(propertyName, value);
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    /// <summary>Sets a color shader property directly.</summary>
    public void SetColor(string propertyName, Color color)
    {
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(propertyName, color);
        _renderer.SetPropertyBlock(_propertyBlock);
    }
}
