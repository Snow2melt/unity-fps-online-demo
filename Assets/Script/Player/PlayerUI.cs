using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{

    public static PlayerUI Singleton;

    private Player player = null;

    [SerializeField]
    private TextMeshProUGUI bulletsText;
    [SerializeField]
    private GameObject bulletsObject;

    private WeaponManager weaponManager;

    [SerializeField]
    private Transform healthBarFill;//改变的是长度
    [SerializeField]
    private GameObject healthBarObject;//改变的是激不激活

    private void Awake()
    {
        Singleton = this;
    }

    public void setPlayer(Player localPlayer)
    {
        player = localPlayer;
        weaponManager = player.GetComponent<WeaponManager>();
        bulletsObject.SetActive(true);
        healthBarObject.SetActive(true);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        if (weaponManager == null) return;
        //var currentWeapon = weaponManager.GetCurrentWeapon();
        var config = weaponManager.GetCurrentWeaponConfig();
        var state = weaponManager.GetCurrentWeaponState();
        if (config == null || state == null) return;

        //if (currentWeapon.isReloading)
        if (state.isReloading)
        {
            bulletsText.text = "Reloading...";
        }
        else
        {
            //bulletsText.text = "Bullets: " + currentWeapon.bullets + "/" + currentWeapon.maxBullets;
            bulletsText.text = "Bullets: " + state.currentBullets + "/" + config.maxBullets;
        }
        healthBarFill.localScale = new Vector3(player.GetHealth() / 100f, 1f, 1f);
    }
}