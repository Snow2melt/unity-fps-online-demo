using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WeaponManager : NetworkBehaviour
{
    [SerializeField]
    private GameObject weaponHolder;

    private PlayerWeapon currentWeapon;
    private WeaponGraphics currentGraphics;
    private AudioSource currentAudioSource;

    [SerializeField]
    private WeaponConfig primaryWeaponConfig;

    [SerializeField]
    private WeaponConfig secondaryWeaponConfig;

    private WeaponState primaryWeaponState = new WeaponState();
    private WeaponState secondaryWeaponState = new WeaponState();

    private WeaponConfig currentWeaponConfig;
    private WeaponState currentWeaponState;

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

        currentWeaponConfig = config;
        currentWeaponState = state;

        for (int i = weaponHolder.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(weaponHolder.transform.GetChild(i).gameObject);
        }

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
        if (currentWeaponConfig == primaryWeaponConfig)
        {
            EquipWeapon(secondaryWeaponConfig, secondaryWeaponState);
        }
        else
        {
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

    public void Reload(WeaponConfig config, WeaponState state)
    {
        if (state.isReloading) return;
        state.isReloading = true;

        print("Reload ... ");

        StartCoroutine(ReloadCoroutine(config, state));
    }

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
        if (slot == 0) return primaryWeaponConfig;
        if (slot == 1) return secondaryWeaponConfig;
        return primaryWeaponConfig;
    }

    public int GetCurrentWeaponSlot()
    {
        EnsureInited();
        return currentWeaponConfig == primaryWeaponConfig ? 0 : 1;
    }
}