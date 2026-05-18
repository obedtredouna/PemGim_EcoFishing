using UnityEngine;

/// <summary>
/// LurePhysics - Attach ke object UMPAN kamu
/// Mengatur efek visual umpan saat di air (gelombang, bubble, dll)
/// </summary>
public class LurePhysics : MonoBehaviour
{
    [Header("=== WATER DETECTION ===")]
    public LayerMask waterLayer;           // Layer air di scene
    public float waterSurfaceOffset = 0f; // Offset posisi Y umpan di permukaan air

    [Header("=== RIPPLE EFFECT (Opsional) ===")]
    public GameObject ripplePrefab;        // Prefab efek ripple/gelombang
    public float rippleInterval = 0.8f;    // Seberapa sering ripple muncul

    [Header("=== VISUAL FEEDBACK ===")]
    public Renderer lureRenderer;          // Renderer umpan
    public Material normalMaterial;        // Material normal
    public Material biteMaterial;          // Material saat ikan gigit (lebih terang/glow)

    // ─── Private ─────────────────────────────────────────────────
    private FishingController fishingController;
    private float rippleTimer = 0f;
    private bool isInWater = false;
    private FishingController.FishingState lastState;

    void Start()
    {
        fishingController = GetComponentInParent<FishingController>();
        if (fishingController == null)
            fishingController = FindFirstObjectByType<FishingController>();

        if (lureRenderer == null)
            lureRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        if (fishingController == null) return;

        var state = fishingController.GetState();

        // Handle state change
        if (state != lastState)
        {
            OnStateChanged(state);
            lastState = state;
        }

        // Spawn ripple saat menunggu
        if (state == FishingController.FishingState.Waiting ||
            state == FishingController.FishingState.Biting)
        {
            UpdateRipple();
        }
    }

    void OnStateChanged(FishingController.FishingState newState)
    {
        switch (newState)
        {
            case FishingController.FishingState.Waiting:
                isInWater = true;
                SetMaterial(normalMaterial);
                break;

            case FishingController.FishingState.Biting:
                SetMaterial(biteMaterial);
                // Spawn ripple besar saat ikan gigit
                SpawnRipple(2f);
                break;

            case FishingController.FishingState.Idle:
                isInWater = false;
                SetMaterial(normalMaterial);
                break;
        }
    }

    void UpdateRipple()
    {
        rippleTimer += Time.deltaTime;
        if (rippleTimer >= rippleInterval)
        {
            rippleTimer = 0f;
            SpawnRipple(1f);
        }
    }

    void SpawnRipple(float scale = 1f)
    {
        if (ripplePrefab == null) return;

        GameObject ripple = Instantiate(
            ripplePrefab,
            transform.position,
            Quaternion.Euler(90f, 0f, 0f) // Flat di permukaan air
        );

        ripple.transform.localScale = Vector3.one * scale;
        Destroy(ripple, 2f); // Auto destroy setelah 2 detik
    }

    void SetMaterial(Material mat)
    {
        if (lureRenderer != null && mat != null)
            lureRenderer.material = mat;
    }
}
