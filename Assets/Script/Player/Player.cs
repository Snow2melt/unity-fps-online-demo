using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField]
    private int maxHealth = 100;
    [SerializeField]
    private Behaviour[] componentsToDisable;
    private bool[] componentsEnabled;
    private bool colliderEnabled;

    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>();

    public NetworkVariable<int> Kills = new NetworkVariable<int>(0);
    public NetworkVariable<int> Deaths = new NetworkVariable<int>(0);

    public int GetKills() => Kills.Value;
    public int GetDeaths() => Deaths.Value;

    [SerializeField] private bool respawnAtCurrentPosition = false;
    private Vector3 initialSpawnPosition;

    [SerializeField] private bool isBot = false;
    public bool IsBot => isBot;

    public void Setup()
    {
        initialSpawnPosition = transform.position;
        componentsEnabled = new bool[componentsToDisable.Length];
        for (int i = 0; i < componentsToDisable.Length; i++)
        {
            componentsEnabled[i] = componentsToDisable[i].enabled;
        }
        Collider col = GetComponent<Collider>();
        colliderEnabled = col.enabled;
        SetDefaults();
    }

    public void SetDefaults()
    {
        for (int i = 0; i < componentsToDisable.Length; i++)
        {
            componentsToDisable[i].enabled = componentsEnabled[i];
        }

        Collider col = GetComponent<Collider>();
        col.enabled = colliderEnabled;

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDead.Value = false;
        }
    }

    public bool IsDead()
    {
        return isDead.Value;
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(3f);

        if (!IsServer) yield break;

        Vector3 spawnPos = GetRespawnPosition();

        SetDefaults();
        RespawnVisualsClientRpc();

        var sendParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        };

        RespawnAtClientRpc(spawnPos, sendParams);

        if (IsHost && IsOwner)
        {
            transform.position = spawnPos;
            GetComponent<PlayerController>()?.ResetMovement();
        }
    }

    [ClientRpc]
    private void RespawnVisualsClientRpc()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
        }

        for (int i = 0; i < componentsToDisable.Length; i++)
        {
            if (componentsToDisable[i] != null)
            {
                componentsToDisable[i].enabled = componentsEnabled[i];
            }
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = colliderEnabled;
        }

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetInteger("direction", 0);
        }

        if (IsBot)
        {
            transform.localScale = Vector3.one;
        }
    }

    public bool TakeDamage(int damage)
    {
        if (!IsServer) return false;
        if (isDead.Value) return false;
        if (damage <= 0) return false;

        Debug.Log($"[TakeDamage-Before] {name} hp={currentHealth.Value}, damage={damage}, isBot={IsBot}");

        currentHealth.Value -= damage;

        Debug.Log($"[TakeDamage-After] {name} hp={currentHealth.Value}");

        if (currentHealth.Value > 0)
        {
            return false;
        }

        currentHealth.Value = 0;
        isDead.Value = true;

        Debug.Log($"[Dead] {name} died. isBot={IsBot}");

        if (!IsHost)
        {
            DieOnServer();
        }

        DieClientRpc();
        return true;
    }

    private void DieOnServer()
    {
        Die();
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        Die();
    }

    private void Die()
    {
        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.StopShooting();
        }

        var anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetInteger("direction", -1);
        }

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
        }

        for (int i = 0; i < componentsToDisable.Length; i++)
        {
            if (componentsToDisable[i] != null)
            {
                componentsToDisable[i].enabled = false;
            }
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (IsBot)
        {
            transform.localScale = new Vector3(1f, 0.2f, 1f);
        }

        if (IsServer)
        {
            StartCoroutine(Respawn());
        }
    }

    public int GetHealth()
    {
        return currentHealth.Value;
    }

    private Vector3 GetRespawnPosition()
    {
        if (respawnAtCurrentPosition)
        {
            return initialSpawnPosition;
        }

        GameObject[] spawns = GameObject.FindGameObjectsWithTag("RespawnPoint");

        if (spawns != null && spawns.Length > 0)
        {
            List<Transform> validSpawns = new List<Transform>();

            foreach (GameObject spawn in spawns)
            {
                if (spawn != null && spawn.activeInHierarchy)
                {
                    validSpawns.Add(spawn.transform);
                }
            }

            if (validSpawns.Count > 0)
            {
                int index = Random.Range(0, validSpawns.Count);
                return validSpawns[index].position;
            }
        }

        Debug.LogWarning("[Respawn] No valid RespawnPoint found, fallback to current position.");
        return initialSpawnPosition;
    }

    [ClientRpc]
    private void RespawnAtClientRpc(Vector3 spawnPos, ClientRpcParams rpcParams = default)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.useGravity = true;
        }

        transform.position = spawnPos;
        GetComponent<PlayerController>()?.ResetMovement();
    }
}