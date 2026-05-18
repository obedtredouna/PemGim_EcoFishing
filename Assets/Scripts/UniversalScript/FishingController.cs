using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FishingController - Attach ke GameObject "FishingRod" (gagang pancing)
/// Mengontrol seluruh mekanik: lempar → tunggu → tarik
/// </summary>
public class FishingController : MonoBehaviour
{
    [Header("=== OBJECTS (Drag dari Hierarchy) ===")]
    public Transform rodTip;          // Empty object di ujung gagang pancing
    public Transform lure;            // Object umpan kamu
    public LineRenderer fishingLine;  // Line Renderer untuk tali

    [Header("=== THROW SETTINGS ===")]
    public float throwForce = 8f;         // Kekuatan lemparan
    public float throwAngle = 35f;        // Sudut lemparan (derajat)
    public LayerMask waterLayer;          // Layer air di scene kamu

    [Header("=== WAITING SETTINGS ===")]
    public float minWaitTime = 2f;        // Waktu minimum sebelum ikan gigit
    public float maxWaitTime = 6f;        // Waktu maksimum sebelum ikan gigit
    public float catchWindow = 1.5f;      // Waktu untuk menekan tombol tarik

    [Header("=== LURE SWAY ===")]
    public float swayAmount = 0.05f;      // Seberapa jauh umpan goyang
    public float swaySpeed = 1.5f;        // Kecepatan goyang

    [Header("=== UI (Opsional, boleh kosong) ===")]
    public TextMeshProUGUI statusText;    // Text status di UI
    public GameObject biteIndicator;     // UI tanda ikan gigit (bisa image seru)

    // ─── Private State ───────────────────────────────────────────
    private FishingState currentState = FishingState.Idle;
    private Vector3 lureTargetPosition;
    private Vector3 lureStartPosition;
    private float lureThrowProgress = 0f;
    private float waitTimer = 0f;
    private float biteTimer = 0f;
    private float targetWaitTime;
    private bool lureLanded = false;
    private Vector3 lureBasePosition; // posisi diam umpan di air

    private Rigidbody lureRb;

    // ─── Enum State ──────────────────────────────────────────────
    public enum FishingState
    {
        Idle,       // Diam, belum mancing
        Throwing,   // Tali sedang dilempar
        Waiting,    // Umpan di air, menunggu ikan
        Biting,     // Ikan gigit! Harus tekan tombol
        Reeling,    // Sedang menarik
        Success,    // Berhasil dapat ikan
        Failed      // Gagal (terlambat tekan)
    }

    // ─────────────────────────────────────────────────────────────
    void Start()
    {
        lureRb = lure.GetComponent<Rigidbody>();

        // Simpan posisi awal umpan (menempel di gagang)
        lureStartPosition = lure.localPosition;

        // Sembunyikan bite indicator di awal
        if (biteIndicator != null)
            biteIndicator.SetActive(false);

        // Setup Line Renderer
        if (fishingLine != null)
        {
            fishingLine.positionCount = 2;
        }

        UpdateStatusText("Tekan [F] untuk melempar");
    }

    void Update()
    {
        HandleInput();
        UpdateLineRenderer();
        UpdateLureSway();
        UpdateStateMachine();
    }

    // ─── INPUT ───────────────────────────────────────────────────
    void HandleInput()
    {
        // Tekan F untuk lempar / tarik
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (currentState == FishingState.Idle)
                StartThrow();
            else if (currentState == FishingState.Biting)
                StartReeling();
            else if (currentState == FishingState.Waiting)
                CancelFishing(); // Tarik paksa tanpa ikan
        }
    }

    // ─── STATE MACHINE ───────────────────────────────────────────
    void UpdateStateMachine()
    {
        switch (currentState)
        {
            case FishingState.Throwing:
                UpdateThrow();
                break;

            case FishingState.Waiting:
                UpdateWaiting();
                break;

            case FishingState.Biting:
                UpdateBiting();
                break;

            case FishingState.Reeling:
                UpdateReeling();
                break;

            case FishingState.Success:
            case FishingState.Failed:
                // Auto reset setelah 2 detik
                waitTimer += Time.deltaTime;
                if (waitTimer > 2f)
                    ResetFishing();
                break;
        }
    }

    // ─── THROW ───────────────────────────────────────────────────
    void StartThrow()
    {
        currentState = FishingState.Throwing;
        lureLanded = false;
        lureThrowProgress = 0f;

        // Hitung titik jatuh umpan di depan player
        Vector3 throwDirection = transform.forward;
        lureTargetPosition = rodTip.position
            + throwDirection * throwForce
            + Vector3.down * 0.5f;

        // Aktifkan physics umpan
        if (lureRb != null)
        {
            lureRb.isKinematic = false;
            Vector3 velocity = CalculateThrowVelocity(rodTip.position, lureTargetPosition, throwAngle);
            lureRb.linearVelocity = velocity;
        }

        lure.SetParent(null); // Lepas dari gagang
        UpdateStatusText("Tali dilempar...");
    }

    void UpdateThrow()
    {
        // Cek apakah umpan sudah menyentuh air / tanah
        if (lureRb != null && lureRb.linearVelocity.magnitude < 0.5f && !lureLanded)
        {
            lureLanded = true;
            LureLanded();
        }
    }

    void LureLanded()
    {
        // Freeze umpan di posisi jatuh
        if (lureRb != null)
        {
            lureRb.isKinematic = true;
        }

        lureBasePosition = lure.position;
        currentState = FishingState.Waiting;

        // Set waktu tunggu random
        targetWaitTime = Random.Range(minWaitTime, maxWaitTime);
        waitTimer = 0f;

        UpdateStatusText("Menunggu ikan... (F = tarik paksa)");
    }

    // ─── WAITING ─────────────────────────────────────────────────
    void UpdateWaiting()
    {
        waitTimer += Time.deltaTime;

        if (waitTimer >= targetWaitTime)
        {
            TriggerBite();
        }
    }

    // ─── BITE ────────────────────────────────────────────────────
    void TriggerBite()
    {
        currentState = FishingState.Biting;
        biteTimer = 0f;

        if (biteIndicator != null)
            biteIndicator.SetActive(true);

        // Goyangin umpan lebih kencang saat ikan gigit
        swayAmount = 0.15f;
        swaySpeed = 5f;

        UpdateStatusText("⚠ IKAN GIGIT! Tekan [F]!");
    }

    void UpdateBiting()
    {
        biteTimer += Time.deltaTime;

        if (biteTimer >= catchWindow)
        {
            // Terlambat!
            FishingFailed();
        }
    }

    // ─── REELING ─────────────────────────────────────────────────
    void StartReeling()
    {
        currentState = FishingState.Reeling;

        if (biteIndicator != null)
            biteIndicator.SetActive(false);

        UpdateStatusText("Menarik... 🎣");
    }

    void UpdateReeling()
    {
        // Tarik umpan kembali ke ujung gagang
        lure.position = Vector3.MoveTowards(
            lure.position,
            rodTip.position,
            Time.deltaTime * 5f
        );

        // Cek apakah sudah sampai
        if (Vector3.Distance(lure.position, rodTip.position) < 0.1f)
        {
            FishingSuccess();
        }
    }

    // ─── SUCCESS / FAIL ──────────────────────────────────────────
    void FishingSuccess()
    {
        currentState = FishingState.Success;
        waitTimer = 0f;
        UpdateStatusText("✅ Dapat ikan!");

        // TODO: Spawn ikan di sini
        // Contoh: GameManager.instance.AddFish(fishType);
    }

    void FishingFailed()
    {
        currentState = FishingState.Failed;
        waitTimer = 0f;

        if (biteIndicator != null)
            biteIndicator.SetActive(false);

        UpdateStatusText("❌ Ikan kabur...");

        // Reset sway
        swayAmount = 0.05f;
        swaySpeed = 1.5f;
    }

    void CancelFishing()
    {
        currentState = FishingState.Reeling;
        UpdateStatusText("Menarik tali...");
    }

    void ResetFishing()
    {
        // Kembalikan umpan ke gagang
        lure.SetParent(transform);
        lure.localPosition = lureStartPosition;
        lure.localRotation = Quaternion.identity;

        if (lureRb != null)
            lureRb.isKinematic = true;

        // Reset sway
        swayAmount = 0.05f;
        swaySpeed = 1.5f;

        currentState = FishingState.Idle;
        UpdateStatusText("Tekan [F] untuk melempar");
    }

    // ─── LURE SWAY ───────────────────────────────────────────────
    void UpdateLureSway()
    {
        if (currentState != FishingState.Waiting && currentState != FishingState.Biting)
            return;

        float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        float swayZ = Mathf.Cos(Time.time * swaySpeed * 0.7f) * swayAmount * 0.5f;

        lure.position = lureBasePosition + new Vector3(swayX, 0f, swayZ);
    }

    // ─── LINE RENDERER ───────────────────────────────────────────
    void UpdateLineRenderer()
    {
        if (fishingLine == null) return;

        fishingLine.SetPosition(0, rodTip.position);
        fishingLine.SetPosition(1, lure.position);
    }

    // ─── HELPER ──────────────────────────────────────────────────
    Vector3 CalculateThrowVelocity(Vector3 from, Vector3 to, float angle)
    {
        Vector3 direction = to - from;
        float distance = new Vector3(direction.x, 0f, direction.z).magnitude;
        float angleRad = angle * Mathf.Deg2Rad;

        float velocityY = Mathf.Tan(angleRad) * distance;
        float time = distance / (Mathf.Cos(angleRad) * throwForce);

        return new Vector3(
            direction.normalized.x * throwForce,
            velocityY / time * 0.5f,
            direction.normalized.z * throwForce
        );
    }

    void UpdateStatusText(string message)
    {
        if (statusText != null)
            statusText.text = message;
        else
            Debug.Log("[Fishing] " + message);
    }

    // ─── PUBLIC GETTER (untuk script lain) ───────────────────────
    public FishingState GetState() => currentState;
    public bool IsIdle() => currentState == FishingState.Idle;
}
