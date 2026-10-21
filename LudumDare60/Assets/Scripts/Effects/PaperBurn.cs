using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaperBurn : MonoBehaviour
{
    [SerializeField] Renderer paperRenderer;
    [SerializeField] TMP_Text[] texts;
    [SerializeField] Shader textBurnShader;
    [SerializeField] Graphic[] images;
    [SerializeField] Shader imageBurnShader;
    [SerializeField] RocketFlight rocketFlight;

    [Header("Горение")]
    [SerializeField] float burnDelay = 0.2f;
    [SerializeField] float burnDuration = 2.5f;
    [SerializeField] Ease burnEase = Ease.InOutSine;
    [SerializeField] bool randomOrigin = true;
    [SerializeField] Vector2 fixedOrigin = new Vector2(0.5f, 0f);

    [Header("Новый лист из факса")]
    [SerializeField] Transform sheetRoot;
    [SerializeField] Transform faxPoint;
    [SerializeField] float restoreDelay = 1f;
    [SerializeField] float printDuration = 1.2f;
    [SerializeField] Ease printEase = Ease.OutSine;
    [SerializeField] AudioSource printSound;
    [SerializeField] bool printOnStart = true;
    [SerializeField] float startDelay = 0.5f;

    [SerializeField, HideInInspector] Vector4 uvRowU = new Vector4(0.1f, 0f, 0f, 0.5f);
    [SerializeField, HideInInspector] Vector4 uvRowV = new Vector4(0f, 0f, 0.1f, 0.5f);

    static readonly int BurnId = Shader.PropertyToID("_Burn");
    static readonly int OriginId = Shader.PropertyToID("_BurnOrigin");
    static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
    static readonly int CharWidthId = Shader.PropertyToID("_CharWidth");
    static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
    static readonly int CharColorId = Shader.PropertyToID("_CharColor");
    static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");

    static readonly int GlobalMatrixId = Shader.PropertyToID("_PB_Matrix");
    static readonly int GlobalBurnId = Shader.PropertyToID("_PB_Burn");
    static readonly int GlobalOriginId = Shader.PropertyToID("_PB_Origin");
    static readonly int GlobalNoiseScaleId = Shader.PropertyToID("_PB_NoiseScale");
    static readonly int GlobalNoiseStrengthId = Shader.PropertyToID("_PB_NoiseStrength");
    static readonly int GlobalCharWidthId = Shader.PropertyToID("_PB_CharWidth");
    static readonly int GlobalEdgeWidthId = Shader.PropertyToID("_PB_EdgeWidth");
    static readonly int GlobalCharColorId = Shader.PropertyToID("_PB_CharColor");
    static readonly int GlobalEdgeColorId = Shader.PropertyToID("_PB_EdgeColor");

    MaterialPropertyBlock block;
    Vector3 basePosition;
    Quaternion baseRotation;
    Sequence sequence;
    Vector2 origin;
    float burn;
    float[] imageAlphas;
    Material[] imageMaterials;
    readonly Dictionary<Material, Material> textMaterials = new Dictionary<Material, Material>();

    void OnValidate()
    {
        if (paperRenderer == null) paperRenderer = GetComponent<Renderer>();
        BuildUvMapping();
    }

    void Awake()
    {
        if (paperRenderer == null) paperRenderer = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        if (sheetRoot == null) sheetRoot = transform;
        basePosition = sheetRoot.localPosition;
        baseRotation = sheetRoot.localRotation;
        BuildUvMapping();
        SetupTextMaterials();
        SetupImageMaterials();
        SetBurn(0f);
    }

    void Start()
    {
        if (!printOnStart || faxPoint == null) return;

        Transform parent = sheetRoot.parent;
        sheetRoot.localPosition = ToLocalPoint(parent, faxPoint.position);
        sheetRoot.localRotation = ToLocalRotation(parent, faxPoint.rotation);

        sequence?.Kill();
        sequence = DOTween.Sequence()
            .AppendInterval(startDelay)
            .AppendCallback(Restore)
            .SetLink(gameObject);
    }

    void OnEnable()
    {
        if (rocketFlight != null) rocketFlight.Arrived += Burn;
    }

    void OnDisable()
    {
        if (rocketFlight != null) rocketFlight.Arrived -= Burn;
        Shader.SetGlobalFloat(GlobalBurnId, 0f);
    }

    void OnDestroy()
    {
        foreach (Material material in textMaterials.Values)
            if (material != null) Destroy(material);
        textMaterials.Clear();

        if (imageMaterials == null) return;
        foreach (Material material in imageMaterials)
            if (material != null) Destroy(material);
    }

    void LateUpdate()
    {
        if (burn > 0f) PushGlobals();
    }

    [ContextMenu("Сжечь")]
    public void Burn()
    {
        sequence?.Kill();

        origin = randomOrigin ? RandomEdgePoint() : fixedOrigin;
        paperRenderer.GetPropertyBlock(block);
        block.SetVector(OriginId, new Vector4(origin.x, origin.y, 0f, 0f));
        paperRenderer.SetPropertyBlock(block);

        SetBurn(0f);
        SetTextAlpha(1f);
        sequence = DOTween.Sequence()
            .AppendInterval(burnDelay)
            .Append(DOTween.To(() => burn, SetBurn, 1f, burnDuration).SetEase(burnEase))
            .AppendInterval(restoreDelay)
            .AppendCallback(Restore)
            .SetLink(gameObject);
    }

    [ContextMenu("Восстановить")]
    public void Restore()
    {
        sequence?.Kill();
        SetBurn(0f);
        SetTextAlpha(1f);

        if (faxPoint == null)
        {
            sheetRoot.localPosition = basePosition;
            sheetRoot.localRotation = baseRotation;
            return;
        }

        Transform parent = sheetRoot.parent;
        Vector3 startPosition = ToLocalPoint(parent, faxPoint.position);
        Quaternion faxRotation = ToLocalRotation(parent, faxPoint.rotation);

        sheetRoot.localPosition = startPosition;
        sheetRoot.localRotation = faxRotation;
        if (printSound != null) printSound.Play();

        sequence = DOTween.Sequence()
            .Append(sheetRoot.DOLocalMove(basePosition, printDuration).SetEase(printEase))
            .Join(sheetRoot.DOLocalRotateQuaternion(baseRotation, printDuration).SetEase(printEase))
            .AppendCallback(StopPrintSound)
            .SetLink(gameObject);
    }

    void StopPrintSound()
    {
        if (printSound != null) printSound.Stop();
    }

    static Vector3 ToLocalPoint(Transform parent, Vector3 worldPoint)
    {
        return parent != null ? parent.InverseTransformPoint(worldPoint) : worldPoint;
    }

    static Quaternion ToLocalRotation(Transform parent, Quaternion worldRotation)
    {
        return parent != null ? Quaternion.Inverse(parent.rotation) * worldRotation : worldRotation;
    }

    void SetBurn(float value)
    {
        burn = value;
        paperRenderer.GetPropertyBlock(block);
        block.SetFloat(BurnId, value);
        paperRenderer.SetPropertyBlock(block);
        PushGlobals();
    }

    void PushGlobals()
    {
        Shader.SetGlobalFloat(GlobalBurnId, burn);
        if (burn <= 0f) return;

        Matrix4x4 uvMatrix = Matrix4x4.identity;
        uvMatrix.SetRow(0, uvRowU);
        uvMatrix.SetRow(1, uvRowV);
        Shader.SetGlobalMatrix(GlobalMatrixId, uvMatrix * paperRenderer.transform.worldToLocalMatrix);
        Shader.SetGlobalVector(GlobalOriginId, new Vector4(origin.x, origin.y, 0f, 0f));

        Material material = paperRenderer.sharedMaterial;
        if (material == null) return;
        Shader.SetGlobalFloat(GlobalNoiseScaleId, material.GetFloat(NoiseScaleId));
        Shader.SetGlobalFloat(GlobalNoiseStrengthId, material.GetFloat(NoiseStrengthId));
        Shader.SetGlobalFloat(GlobalCharWidthId, material.GetFloat(CharWidthId));
        Shader.SetGlobalFloat(GlobalEdgeWidthId, material.GetFloat(EdgeWidthId));
        Shader.SetGlobalColor(GlobalCharColorId, material.GetColor(CharColorId));
        Shader.SetGlobalColor(GlobalEdgeColorId, material.GetColor(EdgeColorId));
    }

    void SetupTextMaterials()
    {
        if (textBurnShader == null || texts == null) return;
        foreach (TMP_Text text in texts)
        {
            if (text == null) continue;
            Material source = text.fontSharedMaterial;
            if (source == null) continue;
            if (!textMaterials.TryGetValue(source, out Material burnMaterial))
            {
                burnMaterial = new Material(source) { shader = textBurnShader };
                textMaterials[source] = burnMaterial;
            }
            text.fontSharedMaterial = burnMaterial;
        }
    }

    void SetupImageMaterials()
    {
        if (images == null) return;
        imageAlphas = new float[images.Length];
        imageMaterials = new Material[images.Length];
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null) continue;
            imageAlphas[i] = images[i].color.a;
            if (imageBurnShader == null) continue;
            imageMaterials[i] = new Material(imageBurnShader);
            images[i].material = imageMaterials[i];
        }
    }

    void SetTextAlpha(float value)
    {
        if (texts != null)
            foreach (TMP_Text text in texts)
                if (text != null) text.alpha = value;

        if (images == null || imageAlphas == null) return;
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null) continue;
            Color color = images[i].color;
            color.a = imageAlphas[i] * value;
            images[i].color = color;
        }
    }

    void BuildUvMapping()
    {
        if (paperRenderer == null) return;
        MeshFilter filter = paperRenderer.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) return;

        Mesh mesh = filter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        if (vertices.Length < 3 || uvs.Length != vertices.Length) return;

        Vector3 meanPosition = Vector3.zero;
        Vector2 meanUv = Vector2.zero;
        for (int i = 0; i < vertices.Length; i++)
        {
            meanPosition += vertices[i];
            meanUv += uvs[i];
        }
        meanPosition /= vertices.Length;
        meanUv /= vertices.Length;

        Matrix4x4 normal = Matrix4x4.zero;
        Vector3 sumU = Vector3.zero;
        Vector3 sumV = Vector3.zero;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 d = vertices[i] - meanPosition;
            Vector2 du = uvs[i] - meanUv;
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    normal[r, c] += d[r] * d[c];
            sumU += d * du.x;
            sumV += d * du.y;
        }

        float trace = normal[0, 0] + normal[1, 1] + normal[2, 2];
        float ridge = Mathf.Max(1e-8f, trace * 1e-6f);
        for (int i = 0; i < 3; i++) normal[i, i] += ridge;
        normal[3, 3] = 1f;

        Matrix4x4 inverse = normal.inverse;
        Vector3 coefU = inverse.MultiplyVector(sumU);
        Vector3 coefV = inverse.MultiplyVector(sumV);

        uvRowU = new Vector4(coefU.x, coefU.y, coefU.z, meanUv.x - Vector3.Dot(coefU, meanPosition));
        uvRowV = new Vector4(coefV.x, coefV.y, coefV.z, meanUv.y - Vector3.Dot(coefV, meanPosition));
    }

    Vector2 RandomEdgePoint()
    {
        switch (Random.Range(0, 4))
        {
            case 0: return new Vector2(Random.value, 0f);
            case 1: return new Vector2(Random.value, 1f);
            case 2: return new Vector2(0f, Random.value);
            default: return new Vector2(1f, Random.value);
        }
    }
}
