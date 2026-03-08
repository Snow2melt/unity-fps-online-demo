[System.Serializable]
public class WeaponState
{
    public int currentBullets;
    public bool isReloading;

    public void Init(WeaponConfig config)
    {
        currentBullets = config.maxBullets;
        isReloading = false;
    }
}