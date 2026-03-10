using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    private const string PLAYER_TAG = "Player";

    private WeaponManager weaponManager;
    //private PlayerWeapon currentWeapon;

    private Camera cam;

    [SerializeField]
    private LayerMask layerMask;

    private float shootCoolDownTime = 0f; //距离上次开枪时间过了多久
    private int autoShootCount = 0; //当前连开多少枪

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

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
/*#if UNITY_EDITOR
        if (!_printedOnce)
        {
            _printedOnce = true;
            Debug.LogError($"[Check] Update running on {name} IsLocalPlayer={IsLocalPlayer} IsOwner={IsOwner} IsServer={IsServer}");
        }
#endif*/

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
            //ShootServerRpc(NetworkManager.Singleton.LocalClientId, transform.name, 1);
            int slot = weaponManager.GetCurrentWeaponSlot();
            ShootRequestServerRpc(NetworkManager.Singleton.LocalClientId, slot, cam.transform.position, cam.transform.forward);
        }
#endif

        var config = weaponManager.GetCurrentWeaponConfig();
        var state = weaponManager.GetCurrentWeaponState();
        if (config == null || state == null) return;

        //currentWeapon = weaponManager.GetCurrentWeapon();

/*#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.K))
        {
            ShootServerRpc(NetworkManager.Singleton.LocalClientId, transform.name, 10000);
        }
#endif*/

        /*if (Input.GetKeyDown(KeyCode.K))
        {
            ShootServerRpc(transform.name, 10000);
        }*/

        if (Input.GetKeyDown(KeyCode.R))
        {
            weaponManager.Reload(config, state);
            return;
        }

        if (config.shootRate <= 0) //单发
        {
            if (Input.GetButtonDown("Fire1") && shootCoolDownTime >= config.shootCoolDownTime)
            {
                autoShootCount = 0;
                //Debug.Log(shootCoolDownTime + " " + currentWeapon.shootCoolDownTime);
                Shoot();
                shootCoolDownTime = 0f; //重置冷却时间
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

    private void OnShoot(float recoilForce) //每次射击相关的逻辑，包括特效、声音等
    {
        //weaponManager.GetCurrentGraphics().muzzleFlash.Play();
        //weaponManager.GetCurrentAudioSource().Play();
        var graphics = weaponManager.GetCurrentGraphics();
        if (graphics != null && graphics.muzzleFlash != null) graphics.muzzleFlash.Play();

        var audio = weaponManager.GetCurrentAudioSource();
        if (audio != null) audio.Play();

        if (IsLocalPlayer) //本地玩家施加后坐力
        {
            playerController.AddRecoilForce(recoilForce);
        }
    }

    [ServerRpc]
    private void OnShootServerRpc(float recoilForce) //每次射击相关的逻辑，包括特效、声音等
    {
        if (!IsHost)
        {
            OnShoot(recoilForce);
        }
        OnShootClientRpc(recoilForce);
    }

    [ClientRpc]
    private void OnShootClientRpc(float recoilForce) //每次射击相关的逻辑，包括特效、声音等
    {
        OnShoot(recoilForce);
    }


    private void OnHit(Vector3 pos, Vector3 normal, HitEffectMaterial material) //击中点的特效
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
        //float recoilForce = currentWeapon.recoilForce;
        float recoilForce = config.recoilForce;
        if (autoShootCount <= 3)
        {
            recoilForce *= 0.1f;
        }

        //OnShootServerRpc(currentWeapon.recoilForce);
        // 本地表现（后坐力/枪口火花/音效）仍然走现有的 RPC
        OnShootServerRpc(recoilForce);

        //核心：不在客户端算命中，只把“射击意图”发给服务器
        //ShootRequestServerRpc(NetworkManager.Singleton.LocalClientId, cam.transform.position, cam.transform.forward);
        int slot = weaponManager.GetCurrentWeaponSlot();
        ShootRequestServerRpc(NetworkManager.Singleton.LocalClientId, slot, cam.transform.position, cam.transform.forward);

        /*RaycastHit hit;
        //if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, currentWeapon.range, layerMask))
        if (Physics.Raycast(
        cam.transform.position,
        cam.transform.forward,
        out hit,
        config.range,
        layerMask,
        QueryTriggerInteraction.Collide))
        {
#if UNITY_EDITOR
            //Debug.Log($"Hit => name:{hit.collider.name}  tag:{hit.collider.tag}  layer:{hit.collider.gameObject.layer}  root:{hit.collider.transform.root.name}");
#endif
            bool hitBody = hit.collider.CompareTag("Player");
            bool hitHead = hit.collider.CompareTag("Head");

            //if (hit.collider.tag == PLAYER_TAG)
            //{
                //ShootServerRpc(hit.collider.name, currentWeapon.damage);
                //ShootServerRpc(hit.collider.name, config.damage);
                //OnHitServerRpc(hit.point, hit.normal, HitEffectMaterial.Metal);
            //}

            if (hitBody || hitHead)
            {
                // 2) 计算最终伤害（头部倍伤）
                int finalDamage = config.damage;
                if (hitHead)
                {
                    finalDamage = Mathf.RoundToInt(config.damage * config.headshotMultiplier);
                    Debug.Log("Headshot!");
                }

                // 3) 关键：拿到玩家 root 的名字（不能用 collider.name）
                // 因为 head hitbox 的 collider.name 可能是 "HeadHitbox"
                string playerName = hit.collider.transform.root.name;

                //ShootServerRpc(NetworkManager.Singleton.LocalClientId, playerName, finalDamage);
                ShootRequestServerRpc(NetworkManager.Singleton.LocalClientId, cam.transform.position, cam.transform.forward);
                OnHitServerRpc(hit.point, hit.normal, HitEffectMaterial.Metal);
            }
            else
            {
                OnHitServerRpc(hit.point, hit.normal, HitEffectMaterial.Stone);
            }
        }*/
    }

    /*[ServerRpc(RequireOwnership = false)]
    //private void ShootServerRpc(string name, int damage)
    private void ShootServerRpc(ulong shooterClientId, string targetName, int damage)
    {
        Debug.LogError($"[Check] ShootServerRpc ENTER shooterClientId={shooterClientId} IsServer={IsServer}");
        if (!IsServer)
        {
            Debug.LogError("ShootServerRpc is not running on server!");
            return;
        }

        bool hasClient = NetworkManager.Singleton.ConnectedClients.ContainsKey(shooterClientId);
        Debug.Log($"[Check] shooterClientId={shooterClientId}, Connected={hasClient}");

        if (hasClient)
        {
            var client = NetworkManager.Singleton.ConnectedClients[shooterClientId];
            var playerObj = client.PlayerObject;
            Debug.Log($"[Check] PlayerObject null? {playerObj == null}");

            if (playerObj != null)
            {
                Debug.Log($"[Check] PlayerObject name={playerObj.name}, pos={playerObj.transform.position}, netId={playerObj.NetworkObjectId}");
            }
        }

        DebugClientRpc($"[ServerCheck] Enter shooterClientId={shooterClientId} IsServer={IsServer}");
        bool ok = NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var client);
        DebugClientRpc($"[ServerCheck] ConnectedClients has shooter? {ok}");
        var po = ok ? client.PlayerObject : null;
        DebugClientRpc($"[ServerCheck] PlayerObject null? {po == null}");

        // 先 return，避免影响你现在逻辑
        return;

        // 1) damage 合法范围（防注入）
        if (damage <= 0 || damage > 200)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[ShootServerRpc] Invalid damage={damage} from shooterClientId={shooterClientId}");
#endif
            return;
        }

        // 2) shooter 必须是有效连接
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(shooterClientId))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[ShootServerRpc] Shooter not connected: shooterClientId={shooterClientId}");
#endif
            return;
        }

        // 3) targetName 不能为空
        if (string.IsNullOrEmpty(targetName))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[ShootServerRpc] Empty targetName from shooterClientId={shooterClientId}");
#endif
            return;
        }

        // 4) target 必须存在
        Player targetPlayer = null;
        try
        {
            targetPlayer = GameManager.Singleton.GetPlayer(targetName);
        }
        catch
        {
            targetPlayer = null;
        }

        if (targetPlayer == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[ShootServerRpc] Target not found: {targetName} from shooterClientId={shooterClientId}");
#endif
            return;
        }

        // 5) 禁止自伤（最基本）
        if (targetPlayer.OwnerClientId == shooterClientId)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[ShootServerRpc] Reject self-damage: shooterClientId={shooterClientId}, target={targetName}");
#endif
            return;
        }

        // 通过校验后才扣血
        targetPlayer.TakeDamage(damage);
    }*/
    [ServerRpc(RequireOwnership = false)]
    private void ShootRequestServerRpc(ulong shooterClientId, int weaponSlot, Vector3 origin, Vector3 direction)
    {
        // 0) shooter 必须有效
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var client)) return;
        var shooterObj = client.PlayerObject;
        if (shooterObj == null) return;

        // 1) 反作弊：方向要接近单位向量
        float mag = direction.magnitude;
        if (mag < 0.9f || mag > 1.1f) return;
        direction /= mag;

        // 2) 反作弊：origin 不允许离 shooter 太远（防伪造）
        if (Vector3.Distance(origin, shooterObj.transform.position) > 2.0f) return;

        // 3) ✅ 服务器读取武器配置（不信客户端传的伤害/射程）
        var wm = shooterObj.GetComponent<WeaponManager>();
        if (wm == null) return;

        var config = wm.GetWeaponConfigBySlot(weaponSlot);
        if (config == null) return;

        float range = config.range;
        int baseDamage = config.damage;
        float headMul = config.headshotMultiplier;

        // 4) 服务器 Raycast
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, range, layerMask, QueryTriggerInteraction.Collide))
        {
            bool hitPlayerBody = hit.collider.CompareTag("Player");
            bool hitPlayerHead = hit.collider.CompareTag("Head");

            bool hitTrainingBody = hit.collider.CompareTag("TrainingTargetBody");
            bool hitTrainingHead = hit.collider.CompareTag("TrainingTargetHead");

            var mat = (hitPlayerBody || hitPlayerHead || hitTrainingBody || hitTrainingHead)
                ? HitEffectMaterial.Metal
                : HitEffectMaterial.Stone;

            OnHitClientRpc(hit.point, hit.normal, mat);

            // 真人玩家
            if (hitPlayerBody || hitPlayerHead)
            {
                Player targetPlayer = hit.collider.GetComponentInParent<Player>();
                int finalDamage = hitPlayerHead
                    ? Mathf.RoundToInt(baseDamage * headMul)
                    : baseDamage;

                ApplyDamageServer(shooterClientId, targetPlayer, finalDamage);
                return;
            }

            // 训练靶
            if (hitTrainingBody || hitTrainingHead)
            {
                TrainingTarget target = hit.collider.GetComponentInParent<TrainingTarget>();
                if (target == null)
                {
                    DebugClientRpc("[TrainingTarget] target is null");
                    return;
                }

                bool killed = target.ApplyDamage(baseDamage, hitTrainingHead);
                DebugClientRpc($"[TrainingTarget] hit={target.name}, head={hitTrainingHead}, killed={killed}");
                return;
            }
        }
    }
    /*
    private void ApplyDamageServer(ulong shooterClientId, Player targetPlayer, int damage)
    {
        if (targetPlayer == null)
        {
            Debug.LogWarning("[ApplyDamage] targetPlayer is null");
            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var shooterClient))
            return;

        var shooterObj = shooterClient.PlayerObject;
        if (shooterObj == null) return;

        if (targetPlayer.gameObject == shooterObj.gameObject) return;

        bool killedThisHit = targetPlayer.TakeDamage(damage);
        if (!killedThisHit) return;

        if (!targetPlayer.IsBot)
        {
            targetPlayer.Deaths.Value += 1;
        }

        var shooterPlayer = shooterObj.GetComponent<Player>();
        if (shooterPlayer == null) return;

        shooterPlayer.Kills.Value += 1;
    }

    */

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