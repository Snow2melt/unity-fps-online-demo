using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    private const string PLAYER_TAG = "Player";

    private WeaponManager weaponManager;
    private Camera cam;

    [SerializeField]
    private LayerMask layerMask;

    private float shootCoolDownTime = 0f;
    private int autoShootCount = 0;

    private PlayerController playerController;

    enum HitEffectMaterial
    {
        Metal,
        Stone,
    }

    private void Awake()
    {
        cam = GetComponentInChildren<Camera>();
        weaponManager = GetComponent<WeaponManager>();
        playerController = GetComponent<PlayerController>();
    }

    void Start()
    {

    }

    void Update()
    {
        shootCoolDownTime += Time.deltaTime;

        if (!IsLocalPlayer) return;

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.LogError($"[Client] P pressed on {name} IsLocalPlayer={IsLocalPlayer} IsOwner={IsOwner}");
            Debug.LogError($"[Client] NetState IsClient={NetworkManager.Singleton.IsClient} IsConnectedClient={NetworkManager.Singleton.IsConnectedClient} IsHost={NetworkManager.Singleton.IsHost} IsServer={NetworkManager.Singleton.IsServer}");
        }
#endif

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.LogError($"[Client] Calling ShootRequestServerRpc from {name}");
            int slot = weaponManager.GetCurrentWeaponSlot();
            ShootRequestServerRpc(NetworkManager.Singleton.LocalClientId, slot, cam.transform.position, cam.transform.forward);
        }
#endif

        var config = weaponManager.GetCurrentWeaponConfig();
        var state = weaponManager.GetCurrentWeaponState();
        if (config == null || state == null) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            weaponManager.Reload(config, state);
            return;
        }

        if (config.shootRate <= 0)
        {
            if (Input.GetButtonDown("Fire1") && shootCoolDownTime >= config.shootCoolDownTime)
            {
                autoShootCount = 0;
                Shoot();
                shootCoolDownTime = 0f;
            }
        }
        else
        {
            if (Input.GetButtonDown("Fire1"))
            {
                autoShootCount = 0;
                InvokeRepeating(nameof(Shoot), 0f, 1f / config.shootRate);
            }
            else if (Input.GetButtonUp("Fire1") || Input.GetKeyDown(KeyCode.Q))
            {
                CancelInvoke(nameof(Shoot));
            }
        }
    }

    public void StopShooting()
    {
        CancelInvoke(nameof(Shoot));
    }

    private void OnShoot(float recoilForce)
    {
        var graphics = weaponManager.GetCurrentGraphics();
        if (graphics != null && graphics.muzzleFlash != null) graphics.muzzleFlash.Play();

        var audio = weaponManager.GetCurrentAudioSource();
        if (audio != null) audio.Play();

        if (IsLocalPlayer)
        {
            playerController.AddRecoilForce(recoilForce);
        }
    }

    [ServerRpc]
    private void OnShootServerRpc(float recoilForce)
    {
        if (!IsHost)
        {
            OnShoot(recoilForce);
        }
        OnShootClientRpc(recoilForce);
    }

    [ClientRpc]
    private void OnShootClientRpc(float recoilForce)
    {
        OnShoot(recoilForce);
    }

    private void OnHit(Vector3 pos, Vector3 normal, HitEffectMaterial material)
    {
        GameObject hitEffectPrefab;
        if (material == HitEffectMaterial.Metal)
        {
            hitEffectPrefab = weaponManager.GetCurrentGraphics().metalHitEffectPrefab;
        }
        else
        {
            hitEffectPrefab = weaponManager.GetCurrentGraphics().stoneHitEffectPrefab;
        }

        GameObject hitEffectObject = Instantiate(hitEffectPrefab, pos, Quaternion.LookRotation(normal));
        ParticleSystem particleSystem = hitEffectObject.GetComponent<ParticleSystem>();
        particleSystem.Emit(1);
        particleSystem.Play();
        Destroy(hitEffectObject, 0.25f);
    }

    [ServerRpc]
    private void OnHitServerRpc(Vector3 pos, Vector3 normal, HitEffectMaterial material)
    {
        if (!IsHost)
        {
            OnHit(pos, normal, material);
        }
        OnHitClientRpc(pos, normal, material);
    }

    [ClientRpc]
    private void OnHitClientRpc(Vector3 pos, Vector3 normal, HitEffectMaterial material)
    {
        OnHit(pos, normal, material);
    }

    private void Shoot()
    {
        var config = weaponManager.GetCurrentWeaponConfig();
        var state = weaponManager.GetCurrentWeaponState();
        if (config == null || state == null) return;
        if (state.currentBullets <= 0 || state.isReloading) return;

        state.currentBullets--;
        if (state.currentBullets <= 0) weaponManager.Reload(config, state);

        autoShootCount++;
        float recoilForce = config.recoilForce;
        if (autoShootCount <= 3)
        {
            recoilForce *= 0.1f;
        }

        OnShootServerRpc(recoilForce);

        int slot = weaponManager.GetCurrentWeaponSlot();
        ShootRequestServerRpc(NetworkManager.Singleton.LocalClientId, slot, cam.transform.position, cam.transform.forward);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShootRequestServerRpc(ulong shooterClientId, int weaponSlot, Vector3 origin, Vector3 direction)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var client)) return;
        var shooterObj = client.PlayerObject;
        if (shooterObj == null) return;

        float mag = direction.magnitude;
        if (mag < 0.9f || mag > 1.1f) return;
        direction /= mag;

        if (Vector3.Distance(origin, shooterObj.transform.position) > 2.0f) return;

        var wm = shooterObj.GetComponent<WeaponManager>();
        if (wm == null) return;

        var config = wm.GetWeaponConfigBySlot(weaponSlot);
        if (config == null) return;

        float range = config.range;
        int baseDamage = config.damage;
        float headMul = config.headshotMultiplier;

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, range, layerMask, QueryTriggerInteraction.Collide))
        {
            bool hitPlayerBody = hit.collider.CompareTag("Player");
            bool hitPlayerHead = hit.collider.CompareTag("Head");

            bool hitTrainingBody = hit.collider.CompareTag("TrainingTargetBody");
            bool hitTrainingHead = hit.collider.CompareTag("TrainingTargetHead");

            bool hitRangeControl = hit.collider.CompareTag("RangeControlButton");

            var mat = (hitPlayerBody || hitPlayerHead || hitTrainingBody || hitTrainingHead || hitRangeControl)
                ? HitEffectMaterial.Metal
                : HitEffectMaterial.Stone;

            OnHitClientRpc(hit.point, hit.normal, mat);

            if (hitPlayerBody || hitPlayerHead)
            {
                Player targetPlayer = hit.collider.GetComponentInParent<Player>();
                int finalDamage = hitPlayerHead
                    ? Mathf.RoundToInt(baseDamage * headMul)
                    : baseDamage;

                ApplyDamageServer(shooterClientId, targetPlayer, finalDamage);
                return;
            }

            if (hitTrainingBody || hitTrainingHead)
            {
                TrainingTarget target = hit.collider.GetComponentInParent<TrainingTarget>();
                if (target == null)
                {
                    DebugClientRpc("[TrainingTarget] target is null");
                    return;
                }

                bool killed = target.ApplyDamage(baseDamage, hitTrainingHead);

                if (killed && RangeSessionManager.Instance != null)
                {
                    if (hitTrainingHead)
                    {
                        RangeSessionManager.Instance.NotifyTargetHeadshotKilled();
                    }
                    else
                    {
                        RangeSessionManager.Instance.NotifyTargetKilled();
                    }
                }

                DebugClientRpc($"[TrainingTarget] hit={target.name}, head={hitTrainingHead}, killed={killed}");
                return;
            }

            if (hitRangeControl)
            {
                RangeControlButton btn = hit.collider.GetComponentInParent<RangeControlButton>();
                if (btn != null)
                {
                    btn.Trigger();
                }
                return;
            }
        }
    }

    private void ApplyDamageServer(ulong shooterClientId, Player targetPlayer, int damage)
    {
        if (targetPlayer == null)
        {
            DebugClientRpc("[ApplyDamage] targetPlayer is null");
            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var shooterClient))
        {
            DebugClientRpc("[ApplyDamage] shooterClient not found");
            return;
        }

        var shooterObj = shooterClient.PlayerObject;
        if (shooterObj == null)
        {
            DebugClientRpc("[ApplyDamage] shooterObj is null");
            return;
        }

        if (targetPlayer.gameObject == shooterObj.gameObject)
        {
            DebugClientRpc("[ApplyDamage] self-hit blocked");
            return;
        }

        DebugClientRpc($"[ApplyDamage] hit {targetPlayer.name}, damage={damage}, isBot={targetPlayer.IsBot}");

        bool killedThisHit = targetPlayer.TakeDamage(damage);

        DebugClientRpc($"[ApplyDamage] killedThisHit={killedThisHit}");

        if (!killedThisHit) return;

        if (!targetPlayer.IsBot)
        {
            targetPlayer.Deaths.Value += 1;
        }

        var shooterPlayer = shooterObj.GetComponent<Player>();
        if (shooterPlayer == null)
        {
            DebugClientRpc("[ApplyDamage] shooterPlayer is null");
            return;
        }

        shooterPlayer.Kills.Value += 1;
        DebugClientRpc($"[ApplyDamage] shooter kills now={shooterPlayer.Kills.Value}");
    }

    [ClientRpc]
    private void DebugClientRpc(string msg)
    {
        Debug.LogError(msg);
    }
}