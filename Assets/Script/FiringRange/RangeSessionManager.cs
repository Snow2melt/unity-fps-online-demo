using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RangeSessionManager : NetworkBehaviour
{
    [Header("Session Config")]
    [SerializeField] private float sessionDuration = 30f;
    [SerializeField] private float targetVisibleDuration = 2f;
    [SerializeField] private float respawnDelayAfterKill = 0.2f;
    [SerializeField] private float respawnDelayAfterMiss = 0.2f;

    [Header("References")]
    [SerializeField] private TrainingTarget trainingTarget;
    [SerializeField] private Transform[] targetSlots;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text killText;
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private TMP_Text headshotText;
    [SerializeField] private TMP_Text stateText;

    private bool sessionRunning = false;
    private int killCount = 0;
    private int headshotCount = 0;
    private float sessionEndTime = 0f;

    private Coroutine sessionCoroutine;
    private int currentSlotIndex = -1;

    private bool targetKilledThisRound = false;

    public static RangeSessionManager Instance { get; private set; }

    public bool IsSessionRunning => sessionRunning;
    public int KillCount => killCount;
    public int BestScore => bestScore;
    public int HeadshotCount => headshotCount;

    private bool forceStopRequested = false;
    private int lastResultKills = 0;
    private int bestScore = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Duplicate RangeSessionManager found, destroying: {name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (timeText != null)
            timeText.text = "TIME  00";

        if (killText != null)
            killText.text = "SCORE  0";

        if (bestText != null)
            bestText.text = "BEST  0";

        if (headshotText != null)
            headshotText.text = "HEAD  0";

        if (stateText != null)
            stateText.text = "READY";
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        if (trainingTarget != null)
        {
            trainingTarget.HideTarget();
        }

        sessionRunning = false;
        killCount = 0;
        headshotCount = 0;
        currentSlotIndex = -1;
        forceStopRequested = false;

        RefreshUIClientRpc(0f, 0, bestScore, 0, "READY");
    }

    private IEnumerator SessionRoutine()
    {
        sessionRunning = true;
        forceStopRequested = false;
        killCount = 0;
        headshotCount = 0;
        sessionEndTime = Time.time + sessionDuration;

        RefreshUIClientRpc(sessionDuration, killCount, bestScore, headshotCount, "RUNNING");

        while (Time.time < sessionEndTime)
        {
            if (forceStopRequested)
                break;

            float remain = Mathf.Max(0f, sessionEndTime - Time.time);

            int slotIndex = GetRandomSlotIndex();
            currentSlotIndex = slotIndex;

            Transform slot = targetSlots[slotIndex];
            trainingTarget.ShowAt(slot.position);

            float targetEndTime = Time.time + targetVisibleDuration;
            bool killedThisRound = false;
            targetKilledThisRound = false;

            while (Time.time < targetEndTime && Time.time < sessionEndTime)
            {
                if (forceStopRequested)
                    break;

                remain = Mathf.Max(0f, sessionEndTime - Time.time);
                RefreshUIClientRpc(remain, killCount, bestScore, headshotCount, "RUNNING");

                if (targetKilledThisRound || trainingTarget.IsDead)
                {
                    killedThisRound = true;
                    killCount++;
                    RefreshUIClientRpc(remain, killCount, bestScore, headshotCount, "RUNNING");
                    break;
                }

                yield return null;
            }

            trainingTarget.HideTarget();

            while (!trainingTarget.IsFullyHidden())
            {
                yield return null;
            }

            if (forceStopRequested || Time.time >= sessionEndTime)
            {
                break;
            }

            yield return new WaitForSeconds(killedThisRound ? respawnDelayAfterKill : respawnDelayAfterMiss);
        }

        trainingTarget.HideTarget();

        while (!trainingTarget.IsFullyHidden())
        {
            yield return null;
        }

        sessionRunning = false;
        currentSlotIndex = -1;
        lastResultKills = killCount;
        bestScore = Mathf.Max(bestScore, killCount);

        RefreshUIClientRpc(0f, killCount, bestScore, headshotCount, "FINISHED");
        sessionCoroutine = null;
    }

    private int GetRandomSlotIndex()
    {
        if (targetSlots == null || targetSlots.Length <= 1)
            return 0;

        int index = Random.Range(0, targetSlots.Length);

        if (index == currentSlotIndex)
            index = (index + 1) % targetSlots.Length;

        return index;
    }

    public void NotifyTargetKilled()
    {
        if (!IsServer) return;
        if (!sessionRunning) return;

        targetKilledThisRound = true;
    }

    public void NotifyTargetHeadshotKilled()
    {
        if (!IsServer) return;
        if (!sessionRunning) return;

        targetKilledThisRound = true;
        headshotCount++;

        Debug.Log($"[RangeSessionManager] Headshot++ on {name}, headshotCount={headshotCount}");
    }

    [ClientRpc]
    private void RefreshUIClientRpc(float remainTime, int kills, int best, int heads, string state)
    {

        Debug.Log($"[RangeSessionManager] RefreshUI on {name}, heads={heads}");
        if (timeText != null)
            timeText.text = $"TIME  {Mathf.CeilToInt(remainTime):00}";

        if (killText != null)
            killText.text = $"SCORE  {kills}";

        if (bestText != null)
            bestText.text = $"BEST  {best}";

        if (headshotText != null)
            headshotText.text = $"HEAD {heads}";

        if (stateText != null)
        {
            switch (state)
            {
                case "READY":
                    stateText.text = "READY";
                    break;
                case "RUNNING":
                    stateText.text = "RUNNING";
                    break;
                case "FINISHED":
                    stateText.text = $"FINISHED  {kills}";
                    break;
                default:
                    stateText.text = state;
                    break;
            }
        }
    }

    private void StartSessionInternal()
    {
        if (!IsServer) return;
        if (sessionRunning) return;
        if (trainingTarget == null) return;
        if (targetSlots == null || targetSlots.Length == 0) return;

        forceStopRequested = false;

        if (sessionCoroutine != null)
            StopCoroutine(sessionCoroutine);

        sessionCoroutine = StartCoroutine(SessionRoutine());
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartSessionServerRpc()
    {
        StartSessionInternal();
    }

    private void StopSessionInternal()
    {
        if (!IsServer) return;
        if (!sessionRunning) return;

        forceStopRequested = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopSessionServerRpc()
    {
        StopSessionInternal();
    }

    public void StartSessionByServer()
    {
        StartSessionInternal();
    }

    public void StopSessionByServer()
    {
        StopSessionInternal();
    }
}