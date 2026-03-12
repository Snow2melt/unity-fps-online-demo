using TMPro;
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
    private Transform healthBarFill;
    [SerializeField]
    private GameObject healthBarObject;

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

    void Start()
    {

    }

    void Update()
    {
        if (player == null) return;
        if (weaponManager == null) return;

        var config = weaponManager.GetCurrentWeaponConfig();
        var state = weaponManager.GetCurrentWeaponState();
        if (config == null || state == null) return;

        if (state.isReloading)
        {
            bulletsText.text = "Reloading...";
        }
        else
        {
            bulletsText.text = "Bullets: " + state.currentBullets + "/" + config.maxBullets;
        }

        healthBarFill.localScale = new Vector3(player.GetHealth() / 100f, 1f, 1f);
    }
}