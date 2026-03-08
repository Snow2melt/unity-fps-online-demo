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

    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(); //修改服务器端

    private NetworkVariable<bool> isDead = new NetworkVariable<bool>();

    // Player.cs 里字段区加
    public NetworkVariable<int> Kills = new NetworkVariable<int>(0);
    public NetworkVariable<int> Deaths = new NetworkVariable<int>(0);

    // 可选：给 UI 用的 getter
    public int GetKills() => Kills.Value;
    public int GetDeaths() => Deaths.Value;


    public void Setup()
    {
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

        if (IsServer) //为什么服务器不动
        //if (IsLocalPlayer)
        {
            currentHealth.Value = maxHealth;
            isDead.Value = false;
        }
    }

    public bool IsDead()
    {
        return isDead.Value;
    }

    /*private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(3f);

        Rigidbody rb = GetComponent<Rigidbody>();

        if (!rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        rb.useGravity = true;

        GetComponent<PlayerController>()?.ResetMovement();

        if (IsServer)
        {
            transform.position = new Vector3(0f, 10f, 0f);
        }

        SetDefaults();

        GetComponentInChildren<Animator>().SetInteger("direction", 0);
    }*/
    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(3f);

        if (!IsServer) yield break;

        Vector3 spawnPos = GetRespawnPosition();

        // 先由服务器恢复真正的生命状态
        SetDefaults();

        // 所有人都恢复这个玩家的表现状态
        RespawnVisualsClientRpc();

        // 再单独通知 owner 去传送
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
            componentsToDisable[i].enabled = componentsEnabled[i];
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
    }


    /*public void TakeDamage(int damage)//受到了多少伤害,只会在服务器段调用
    {
        if (isDead.Value) return;

        currentHealth.Value -= damage;
        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            isDead.Value = true;

            if (!IsHost)
            {
                DieOnServer();
            }
            DieClientRpc();//发到每一个客户端上，三个客户端的调用·
        }
    }*/

    public bool TakeDamage(int damage) // 只在服务器端调用；返回“这次是否首次击杀成功”
    {
        if (!IsServer) return false;
        if (isDead.Value) return false;
        if (damage <= 0) return false;

        currentHealth.Value -= damage;

        if (currentHealth.Value > 0)
        {
            return false;
        }

        currentHealth.Value = 0;
        isDead.Value = true;

        if (!IsHost)
        {
            DieOnServer();
        }

        DieClientRpc(); // 发到每一个客户端上
        return true;    // 只有 alive -> dead 这一瞬间才会返回 true
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
        GetComponent<PlayerShooting>().StopShooting();

        GetComponentInChildren<Animator>().SetInteger("direction", -1);  //动态获取
        GetComponent<Rigidbody>().useGravity = false;

        for (int i = 0; i < componentsToDisable.Length; i++)
        {
            componentsToDisable[i].enabled = false;
        }
        Collider col = GetComponent<Collider>();
        col.enabled = false;

        //StartCoroutine(Respawn());
        if (IsServer)
        {
            StartCoroutine(Respawn());
        }
    }

    public int GetHealth()
    {
        return currentHealth.Value;
    }

    /*private Vector3 GetRespawnPosition()
    {
        GameObject spawn = GameObject.Find("SpawnPoint");
        if (spawn != null)
        {
            return spawn.transform.position;
        }

        return new Vector3(0f, 10f, 0f);
    }*/
    private Vector3 GetRespawnPosition()
    {
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

        Debug.LogWarning("[Respawn] No valid RespawnPoint found, fallback to default position.");
        return new Vector3(0f, 10f, 0f);
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