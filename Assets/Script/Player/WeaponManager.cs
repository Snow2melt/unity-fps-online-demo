using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WeaponManager : NetworkBehaviour
{
    //[SerializeField]
    //private PlayerWeapon primaryWeapon;

    [SerializeField]
    private GameObject weaponHolder;

    private PlayerWeapon currentWeapon;

    //[SerializeField]
    //private PlayerWeapon secondaryWeapon;
    private WeaponGraphics currentGraphics;
    private AudioSource currentAudioSource;

    //新增
    [SerializeField]
    private WeaponConfig primaryWeaponConfig;

    [SerializeField]
    private WeaponConfig secondaryWeaponConfig;

    private WeaponState primaryWeaponState = new WeaponState();
    private WeaponState secondaryWeaponState = new WeaponState();

    private WeaponConfig currentWeaponConfig;
    private WeaponState currentWeaponState;

    // Start is called before the first frame update 

    private bool inited = false;

    private void EnsureInited()
    {
        if (inited) return;
        if (primaryWeaponConfig == null || secondaryWeaponConfig == null) return;

        primaryWeaponState.Init(primaryWeaponConfig);
        secondaryWeaponState.Init(secondaryWeaponConfig);
        EquipWeapon(primaryWeaponConfig, primaryWeaponState);
        inited = true;
    }

    void Start()
    {
        //EquipWeapon(primaryWeapon);
        //primaryWeaponState.Init(primaryWeaponConfig);
        //secondaryWeaponState.Init(secondaryWeaponConfig);

       // EquipWeapon(primaryWeaponConfig, primaryWeaponState);

        if (primaryWeaponConfig == null)
        {
            Debug.LogError("[WeaponManager] primaryWeaponConfig is NULL");
            enabled = false;
            return;
        }

        primaryWeaponState.Init(primaryWeaponConfig);

        if (secondaryWeaponConfig != null)
            secondaryWeaponState.Init(secondaryWeaponConfig);

        EquipWeapon(primaryWeaponConfig, primaryWeaponState);
    }

    /*public void EquipWeapon(PlayerWeapon weapon)
    {
        currentWeapon = weapon;

        while (weaponHolder.transform.childCount > 0)
        {
            DestroyImmediate(weaponHolder.transform.GetChild(0).gameObject);
        }

        GameObject weaponObject = Instantiate(currentWeapon.graphics, weaponHolder.transform.position, weaponHolder.transform.rotation);
        weaponObject.transform.SetParent(weaponHolder.transform);

        currentGraphics = weaponObject.GetComponent<WeaponGraphics>();
        currentAudioSource = weaponObject.GetComponent<AudioSource>();
        if (IsLocalPlayer)
        {
            currentAudioSource.spatialBlend = 0f; //防止自己在右边
        }    
    }*/

    /*public void EquipWeapon(WeaponConfig config, WeaponState state)
    {
        if (currentWeaponConfig.graphics == null)
        {
            Debug.LogError($"[WeaponManager] graphics is NULL for config={currentWeaponConfig.weaponName}");
            return;
        }

        currentWeaponConfig = config;
        currentWeaponState = state;

        while (weaponHolder.transform.childCount > 0)
        {
            Destroy(weaponHolder.transform.GetChild(0).gameObject);
        }

        GameObject weaponObject = Instantiate(currentWeaponConfig.graphics, weaponHolder.transform.position, weaponHolder.transform.rotation);
        weaponObject.transform.SetParent(weaponHolder.transform);

        currentGraphics = weaponObject.GetComponent<WeaponGraphics>();
        currentAudioSource = weaponObject.GetComponent<AudioSource>();

        if (currentAudioSource != null && IsLocalPlayer)
            currentAudioSource.spatialBlend = 0f;

        if (IsLocalPlayer)
        {
            currentAudioSource.spatialBlend = 0f;
        }
    }*/

    public void EquipWeapon(WeaponConfig config, WeaponState state)
    {
        if (config == null)
        {
            Debug.LogError("[WeaponManager] EquipWeapon failed: config is NULL", this);
            return;
        }
        if (state == null)
        {
            Debug.LogError("[WeaponManager] EquipWeapon failed: state is NULL", this);
            return;
        }
        if (weaponHolder == null)
        {
            Debug.LogError($"[WeaponManager] EquipWeapon failed: weaponHolder is NULL (config={config.weaponName})", this);
            return;
        }
        if (config.graphics == null)
        {
            Debug.LogError($"[WeaponManager] EquipWeapon failed: config.graphics is NULL (config={config.weaponName})", this);
            return;
        }

        // 先赋值（之后再用 currentWeaponConfig）
        currentWeaponConfig = config;
        currentWeaponState = state;

        // 清空旧武器
        for (int i = weaponHolder.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(weaponHolder.transform.GetChild(i).gameObject);
        }

        // 实例化新武器（建议用 SetParent(false) 避免偏移）
        GameObject weaponObject = Instantiate(config.graphics);
        weaponObject.transform.SetParent(weaponHolder.transform, false);
        weaponObject.transform.localPosition = Vector3.zero;
        weaponObject.transform.localRotation = Quaternion.identity;

        currentGraphics = weaponObject.GetComponent<WeaponGraphics>();
        currentAudioSource = weaponObject.GetComponent<AudioSource>();

        if (IsLocalPlayer && currentAudioSource != null)
            currentAudioSource.spatialBlend = 0f;
    }

    public PlayerWeapon GetCurrentWeapon()
    {
        return currentWeapon;
    }

    public WeaponGraphics GetCurrentGraphics()
    {
        return currentGraphics;
    }

    public AudioSource GetCurrentAudioSource()
    {
        return currentAudioSource;
    }

    public void ToggleWeapon()
    {
        //if (currentWeapon == primaryWeapon)
        if (currentWeaponConfig == primaryWeaponConfig)
        {
            //EquipWeapon(secondaryWeapon);
            EquipWeapon(secondaryWeaponConfig, secondaryWeaponState);
        }
        else
        {
            //EquipWeapon(primaryWeapon);
            EquipWeapon(primaryWeaponConfig, primaryWeaponState);
        }
    }

    [ClientRpc]
    private void ToggleWeaponClientRpc()
    {
        ToggleWeapon();
    }


    [ServerRpc]
    private void ToggleWeaponServerRpc()
    {
        if (!IsHost)
        {
            ToggleWeapon();
        }
        ToggleWeaponClientRpc();
    }

    // Update is called once per frame
    void Update()
    {
        if (IsLocalPlayer)
        {

            if (Input.GetKeyUp(KeyCode.Q))
            {
                ToggleWeaponServerRpc();
            }
        }
    }

    //public void Reload(PlayerWeapon playerWeapon)
    public void Reload(WeaponConfig config, WeaponState state)
    {
        //if (playerWeapon.isReloading) return;
        //playerWeapon.isReloading = true;
        if (state.isReloading) return;
        state.isReloading = true;

        print("Reload ... ");

        //StartCoroutine(ReloadCoroutine(playerWeapon));
        StartCoroutine(ReloadCoroutine(config, state));
    }

    /*private IEnumerator ReloadCoroutine(PlayerWeapon playerWeapon)
    {
        yield return new WaitForSeconds(playerWeapon.reloadTime);

        playerWeapon.bullets = playerWeapon.maxBullets;

        playerWeapon.isReloading = false;
    }*/

    private IEnumerator ReloadCoroutine(WeaponConfig config, WeaponState state)
    {
        yield return new WaitForSeconds(config.reloadTime);

        state.currentBullets = config.maxBullets;
        state.isReloading = false;
    }

    public WeaponConfig GetCurrentWeaponConfig()
    {
        EnsureInited();
        return currentWeaponConfig;
    }

    public WeaponState GetCurrentWeaponState()
    {
        EnsureInited();
        return currentWeaponState;
    }

    public WeaponConfig GetWeaponConfigBySlot(int slot)
    {
        EnsureInited();
        // slot: 0=主武器 1=副武器
        if (slot == 0) return primaryWeaponConfig;
        if (slot == 1) return secondaryWeaponConfig;
        return primaryWeaponConfig;
    }

    public int GetCurrentWeaponSlot()
    {
        EnsureInited();
        // 当前装备的是哪把
        return currentWeaponConfig == primaryWeaponConfig ? 0 : 1;
    }
}
