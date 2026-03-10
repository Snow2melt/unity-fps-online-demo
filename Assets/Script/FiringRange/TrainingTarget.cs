using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class TrainingTarget : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float headshotMultiplier = 2f;

    [Header("Optional References")]
    [SerializeField] private Collider bodyCollider;
    [SerializeField] private Collider headCollider;
    [SerializeField] private GameObject visualRoot;


    [SerializeField] private float hiddenYOffset = 3.5f;   // 地下多深
    [SerializeField] private float riseDuration = 0.22f;   // 升起时间
    [SerializeField] private float fallDuration = 0.16f;   // 下沉时间

    private Coroutine moveCoroutine;
    private Vector3 visiblePosition;
    private Vector3 hiddenPosition;

    private enum TargetState
    {
        Hidden,
        Rising,
        Visible,
        Falling
    }

    private TargetState currentState = TargetState.Hidden;

    private int currentHealth;
    private bool isActiveTarget = false;
    private bool isDead = false;

    public bool IsActiveTarget => isActiveTarget;

    private NetworkVariable<bool> netVisible = new NetworkVariable<bool>(false);
    public bool IsDead => isDead;

    private void Awake()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider>();
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[TrainingTarget] OnNetworkSpawn | IsServer={IsServer} | IsClient={IsClient} | IsSpawned={IsSpawned}");

        netVisible.OnValueChanged += OnVisibleChanged;

        if (IsServer)
        {
            currentHealth = maxHealth;
            isDead = false;
            isActiveTarget = false;
            currentState = TargetState.Hidden;
            netVisible.Value = false;
            ApplyVisibleState(false);
        }
        else
        {
            ApplyVisibleState(netVisible.Value);
        }
    }

    public void ShowAt(Vector3 position)
    {
        Debug.Log($"[TrainingTarget] ShowAt called | IsServer={IsServer} | pos={position}");

        if (!IsServer) return;

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        visiblePosition = position;
        hiddenPosition = position + Vector3.down * hiddenYOffset;

        transform.position = hiddenPosition;
        currentHealth = maxHealth;
        isDead = false;
        isActiveTarget = false; // 升起过程中先不给打
        currentState = TargetState.Rising;
        netVisible.Value = true;

        moveCoroutine = StartCoroutine(RiseRoutine());
    }

    public void HideTarget()
    {
        Debug.Log($"[TrainingTarget] HideTarget called | IsServer={IsServer}");

        if (!IsServer) return;

        if (currentState == TargetState.Hidden || currentState == TargetState.Falling)
        {
            return;
        }

        isActiveTarget = false;

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        currentState = TargetState.Falling;
        moveCoroutine = StartCoroutine(FallRoutine());
    }

    public bool ApplyDamage(int baseDamage, bool hitHead)
    {
        if (!IsServer) return false;
        if (!isActiveTarget) return false;
        if (isDead) return false;
        if (baseDamage <= 0) return false;

        int finalDamage = hitHead
            ? Mathf.RoundToInt(baseDamage * headshotMultiplier)
            : baseDamage;

        currentHealth -= finalDamage;

        if (currentHealth > 0)
        {
            return false;
        }

        currentHealth = 0;
        isDead = true;
        isActiveTarget = false;

        if (RangeSessionManager.Instance != null)
        {
            RangeSessionManager.Instance.NotifyTargetKilled();
        }

        HideTarget();
        return true;
    }

    private void OnVisibleChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[TrainingTarget] OnVisibleChanged old={oldValue} new={newValue}");

        ApplyVisibleState(newValue);
    }

    private void ApplyVisibleState(bool visible)
    {
        if (bodyCollider != null) bodyCollider.enabled = visible;
        if (headCollider != null) headCollider.enabled = visible;
        if (visualRoot != null) visualRoot.SetActive(visible);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        netVisible.OnValueChanged -= OnVisibleChanged;
    }
    private IEnumerator RiseRoutine()
    {
        float elapsed = 0f;
        Vector3 start = hiddenPosition;
        Vector3 end = visiblePosition;

        ApplyVisibleState(false);   // 一开始先隐藏

        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);
            transform.position = Vector3.Lerp(start, end, t);

            // 快接近地面时再显示
            if (t >= 0.85f && (visualRoot == null || !visualRoot.activeSelf))
            {
                ApplyVisibleState(true);
            }

            yield return null;
        }

        transform.position = end;
        ApplyVisibleState(true);
        isActiveTarget = true;
        currentState = TargetState.Visible;
        moveCoroutine = null;
    }
    private IEnumerator FallRoutine()
    {
        float elapsed = 0f;
        Vector3 start = transform.position;
        Vector3 end = visiblePosition + Vector3.down * hiddenYOffset;

        bool hiddenApplied = false;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);
            transform.position = Vector3.Lerp(start, end, t);

            // 下沉到 25% 左右就隐藏，后面继续在地下移动但用户看不见
            if (t >= 0.08f && !hiddenApplied)
            {
                ApplyVisibleState(false);
                hiddenApplied = true;
            }

            yield return null;
        }

        transform.position = end;
        netVisible.Value = false;
        currentState = TargetState.Hidden;
        moveCoroutine = null;
    }

    public bool IsFullyHidden()
    {
        return currentState == TargetState.Hidden;
    }
}