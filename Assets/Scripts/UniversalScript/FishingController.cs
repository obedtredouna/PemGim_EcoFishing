using System;
using UnityEngine;
using UnityEngine.Events;

public class FishingController : MonoBehaviour
{
    [Header("=== OBJECTS ===")]
    public Transform rodTip;
    public Transform lure;
    public LineRenderer fishingLine;

    [Header("=== THROW SETTINGS ===")]
    public float throwForce = 8f;
    public float throwAngle = 35f;

    [Header("=== WAIT SETTINGS ===")]
    public float minWaitTime = 2f;
    public float maxWaitTime = 6f;
    public float catchWindow = 1.5f;

    [Header("=== LURE SWAY ===")]
    public float swayAmount = 0.05f;
    public float swaySpeed = 1.5f;

    [Header("=== STATE EVENTS ===")]
    public UnityEvent onIdle;
    public UnityEvent onThrowing;
    public UnityEvent onWaiting;
    public UnityEvent onBiting;
    public UnityEvent onReeling;
    public UnityEvent onSuccess;
    public UnityEvent onFailed;

    [Header("=== GENERAL EVENTS ===")]
    public StringEvent onStatusChanged;
    public FishingStateEvent onStateChanged;

    [Serializable]
    public class StringEvent : UnityEvent<string> { }

    [Serializable]
    public class FishingStateEvent : UnityEvent<FishingState> { }

    public enum FishingState
    {
        Idle,
        Throwing,
        Waiting,
        Biting,
        Reeling,
        Success,
        Failed
    }

    private FishingState currentState = FishingState.Idle;

    private Rigidbody lureRb;

    private Vector3 lureStartLocalPosition;
    private Quaternion lureStartLocalRotation;

    private Vector3 lureBasePosition;
    private Vector3 lureTargetPosition;

    private float waitTimer;
    private float biteTimer;
    private float targetWaitTime;

    private bool lureLanded;

    // ─────────────────────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        lureRb = lure.GetComponent<Rigidbody>();

        lureStartLocalPosition = lure.localPosition;
        lureStartLocalRotation = lure.localRotation;

        if (fishingLine != null)
        {
            fishingLine.positionCount = 2;
        }

        SetState(FishingState.Idle);

        SendStatus("Press [F] to Fish");
    }

    void Update()
    {
        HandleInput();
        UpdateStateMachine();
        UpdateLineRenderer();
        UpdateLureSway();
    }

    // ─────────────────────────────────────────────────────────────
    // INPUT
    // ─────────────────────────────────────────────────────────────

    void HandleInput()
    {
        if (!Input.GetKeyDown(KeyCode.F))
            return;

        switch (currentState)
        {
            case FishingState.Idle:
                StartThrow();
                break;

            case FishingState.Waiting:
                CancelFishing();
                break;

            case FishingState.Biting:
                StartReeling();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // STATE MACHINE
    // ─────────────────────────────────────────────────────────────

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

                waitTimer += Time.deltaTime;

                if (waitTimer >= 2f)
                {
                    ResetFishing();
                }

                break;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // THROW
    // ─────────────────────────────────────────────────────────────

    void StartThrow()
    {
        SetState(FishingState.Throwing);

        lureLanded = false;

        Vector3 throwDirection = transform.forward;

        lureTargetPosition =
            rodTip.position +
            throwDirection * throwForce +
            Vector3.down * 0.5f;

        lure.SetParent(null);

        if (lureRb != null)
        {
            lureRb.isKinematic = false;

            Vector3 velocity =
                CalculateThrowVelocity(
                    rodTip.position,
                    lureTargetPosition,
                    throwAngle
                );

            lureRb.linearVelocity = velocity;
        }

        SendStatus("Casting...");
    }

    void UpdateThrow()
    {
        if (lureRb == null)
            return;

        if (lureRb.linearVelocity.magnitude < 0.5f && !lureLanded)
        {
            lureLanded = true;
            LureLanded();
        }
    }

    void LureLanded()
    {
        if (lureRb != null)
        {
            lureRb.isKinematic = true;
        }

        lureBasePosition = lure.position;

        targetWaitTime = UnityEngine.Random.Range(minWaitTime, maxWaitTime);

        waitTimer = 0f;

        SetState(FishingState.Waiting);

        SendStatus("Waiting for fish...");
    }

    // ─────────────────────────────────────────────────────────────
    // WAITING
    // ─────────────────────────────────────────────────────────────

    void UpdateWaiting()
    {
        waitTimer += Time.deltaTime;

        if (waitTimer >= targetWaitTime)
        {
            TriggerFishBite();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // BITE
    // ─────────────────────────────────────────────────────────────

    void TriggerFishBite()
    {
        SetState(FishingState.Biting);

        biteTimer = 0f;

        SendStatus("FISH BITE! PRESS [F]!");
    }

    void UpdateBiting()
    {
        biteTimer += Time.deltaTime;

        if (biteTimer >= catchWindow)
        {
            FishingFailed();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // REELING
    // ─────────────────────────────────────────────────────────────

    void StartReeling()
    {
        SetState(FishingState.Reeling);

        SendStatus("Reeling...");
    }

    void UpdateReeling()
    {
        lure.position = Vector3.MoveTowards(
            lure.position,
            rodTip.position,
            Time.deltaTime * 5f
        );

        if (Vector3.Distance(lure.position, rodTip.position) < 0.1f)
        {
            FishingSuccess();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // RESULT
    // ─────────────────────────────────────────────────────────────

    void FishingSuccess()
    {
        SetState(FishingState.Success);

        waitTimer = 0f;

        SendStatus("Fish Caught!");
    }

    void FishingFailed()
    {
        SetState(FishingState.Failed);

        waitTimer = 0f;

        SendStatus("Fish Escaped!");
    }

    void CancelFishing()
    {
        SetState(FishingState.Reeling);

        SendStatus("Cancelled Fishing");
    }

    // ─────────────────────────────────────────────────────────────
    // RESET
    // ─────────────────────────────────────────────────────────────

    void ResetFishing()
    {
        lure.SetParent(transform);

        lure.localPosition = lureStartLocalPosition;
        lure.localRotation = lureStartLocalRotation;

        if (lureRb != null)
        {
            lureRb.isKinematic = true;
            lureRb.linearVelocity = Vector3.zero;
            lureRb.angularVelocity = Vector3.zero;
        }

        SetState(FishingState.Idle);

        SendStatus("Press [F] to Fish");
    }

    // ─────────────────────────────────────────────────────────────
    // SWAY
    // ─────────────────────────────────────────────────────────────

    void UpdateLureSway()
    {
        if (currentState != FishingState.Waiting &&
            currentState != FishingState.Biting)
            return;

        float swayX =
            Mathf.Sin(Time.time * swaySpeed) * swayAmount;

        float swayZ =
            Mathf.Cos(Time.time * swaySpeed * 0.7f)
            * swayAmount
            * 0.5f;

        lure.position =
            lureBasePosition +
            new Vector3(swayX, 0f, swayZ);
    }

    // ─────────────────────────────────────────────────────────────
    // LINE
    // ─────────────────────────────────────────────────────────────

    void UpdateLineRenderer()
    {
        if (fishingLine == null)
            return;

        fishingLine.SetPosition(0, rodTip.position);
        fishingLine.SetPosition(1, lure.position);
    }

    // ─────────────────────────────────────────────────────────────
    // STATE EVENTS
    // ─────────────────────────────────────────────────────────────

    void SetState(FishingState newState)
    {
        currentState = newState;

        onStateChanged?.Invoke(currentState);

        switch (currentState)
        {
            case FishingState.Idle:
                onIdle?.Invoke();
                break;

            case FishingState.Throwing:
                onThrowing?.Invoke();
                break;

            case FishingState.Waiting:
                onWaiting?.Invoke();
                break;

            case FishingState.Biting:
                onBiting?.Invoke();
                break;

            case FishingState.Reeling:
                onReeling?.Invoke();
                break;

            case FishingState.Success:
                onSuccess?.Invoke();
                break;

            case FishingState.Failed:
                onFailed?.Invoke();
                break;
        }
    }

    void SendStatus(string message)
    {
        Debug.Log("[Fishing] " + message);

        onStatusChanged?.Invoke(message);
    }

    // ─────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────

    Vector3 CalculateThrowVelocity(
        Vector3 from,
        Vector3 to,
        float angle
    )
    {
        Vector3 direction = to - from;

        float distance =
            new Vector3(direction.x, 0f, direction.z).magnitude;

        float angleRad = angle * Mathf.Deg2Rad;

        float velocityY =
            Mathf.Tan(angleRad) * distance;

        float time =
            distance /
            (Mathf.Cos(angleRad) * throwForce);

        return new Vector3(
            direction.normalized.x * throwForce,
            velocityY / time * 0.5f,
            direction.normalized.z * throwForce
        );
    }

    // ─────────────────────────────────────────────────────────────
    // PUBLIC
    // ─────────────────────────────────────────────────────────────

    public FishingState GetState()
    {
        return currentState;
    }

    public bool IsIdle()
    {
        return currentState == FishingState.Idle;
    }
}