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
    [SerializeField] private TMP_Text stateText;

    private bool sessionRunning = false;
    private int killCount = 0;
    private float sessionEndTime = 0f;

    private Coroutine sessionCoroutine;
    private int currentSlotIndex = -1;

    private bool targetKilledThisRound = false;

    public static RangeSessionManager Instance { get; private set; }

    public bool IsSessionRunning => sessionRunning;
    public int KillCount => killCount;

    private bool forceStopRequested = false;
    private int lastResultKills = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate RangeSessionManager found.");
        }


        Instance = this;

        if (timeText != null)
        {
            timeText.text = "TIME: 0";
        }

        if (killText != null)
        {
            killText.text = "KILLS: 0";
        }

        if (stateText != null)
        {
            stateText.text = "READY";
        }
    }

    /*public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (trainingTarget != null)
            {
                trainingTarget.HideTarget();
            }
        }
    }*/

    private IEnumerator SessionRoutine()
    {
        sessionRunning = true;
        forceStopRequested = false;
        killCount = 0;
        sessionEndTime = Time.time + sessionDuration;

        RefreshUIClientRpc(sessionDuration, killCount, "RUNNING");

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
                RefreshUIClientRpc(remain, killCount, "RUNNING");

                if (targetKilledThisRound || trainingTarget.IsDead)
                {
                    killedThisRound = true;
                    killCount++;
                    RefreshUIClientRpc(remain, killCount, "RUNNING");
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

        RefreshUIClientRpc(0f, killCount, "FINISHED");
        sessionCoroutine = null;
    }

    private int GetRandomSlotIndex()
    {
        if (targetSlots.Length <= 1)
        {
            return 0;
        }

        int index = Random.Range(0, targetSlots.Length);

        if (index == currentSlotIndex)
        {
            index = (index + 1) % targetSlots.Length;
        }

        return index;
    }

    public void NotifyTargetKilled()
    {
        if (!IsServer) return;
        if (!sessionRunning) return;

        targetKilledThisRound = true;
    }

    [ClientRpc]
    private void RefreshUIClientRpc(float remainTime, int kills, string state)
    {
        if (timeText != null)
        {
            timeText.text = $"TIME  {Mathf.CeilToInt(remainTime):00}";
        }

        if (killText != null)
        {
            killText.text = $"SCORE  {kills}";
        }

        if (stateText != null)
        {
            switch (state)
            {
                case "READY":
                    stateText.text = "SHOOT START TO BEGIN";
                    break;
                case "RUNNING":
                    stateText.text = "SESSION RUNNING";
                    break;
                case "FINISHED":
                    stateText.text = $"FINISHED  SCORE {kills}";
                    break;
                default:
                    stateText.text = state;
                    break;
            }
        }
    }
    /*
    private void StartSessionInternal()
    {
        Debug.Log($"[RangeSessionManager] StartSessionInternal | IsServer={IsServer} | sessionRunning={sessionRunning} | trainingTargetNull={trainingTarget == null} | slotCount={(targetSlots == null ? -1 : targetSlots.Length)}");

        if (!IsServer) return;
        if (sessionRunning) return;
        if (trainingTarget == null) return;
        if (targetSlots == null || targetSlots.Length == 0) return;

        if (sessionCoroutine != null)
        {
            StopCoroutine(sessionCoroutine);
        }

        sessionCoroutine = StartCoroutine(SessionRoutine());
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartSessionServerRpc()
    {
        StartSessionInternal();
    }*/

    /*public override void OnNetworkSpawn()
    {
        Debug.Log($"[RangeSessionManager] OnNetworkSpawn | scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} | IsServer={IsServer} | IsClient={IsClient} | IsSpawned={IsSpawned}");

        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (trainingTarget != null)
            {
                Debug.Log("[RangeSessionManager] Hide target on spawn");
                trainingTarget.HideTarget();
            }
            else
            {
                Debug.LogError("[RangeSessionManager] trainingTarget is NULL");
            }

            Debug.Log("[RangeSessionManager] Start AutoStartAfterDelay");
            StartCoroutine(AutoStartAfterDelay());
        }
    }

    private IEnumerator AutoStartAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        Debug.Log("[RangeSessionManager] AutoStartAfterDelay finished, calling StartSessionInternal");
        StartSessionInternal();
    }*/
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
        currentSlotIndex = -1;

        RefreshUIClientRpc(0f, 0, "READY");
    }
    private void StartSessionInternal()
    {
        if (!IsServer) return;
        if (sessionRunning) return;
        if (trainingTarget == null) return;
        if (targetSlots == null || targetSlots.Length == 0) return;

        forceStopRequested = false;

        if (sessionCoroutine != null)
        {
            StopCoroutine(sessionCoroutine);
        }

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