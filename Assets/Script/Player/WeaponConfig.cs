using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "Weapons/Weapon Config")]
public class WeaponConfig : ScriptableObject
{
    public string weaponName = "Colt";
    public int damage = 10;
    public float range = 100f;

    public float shootRate = 10f;
    public float shootCoolDownTime = 0.75f;
    public float recoilForce = 2f;

    public int maxBullets = 30;
    public float reloadTime = 2f;

    public float headshotMultiplier = 2f;

    public GameObject graphics;
}