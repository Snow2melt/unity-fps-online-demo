using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerWeapon
{
    
    public string name = "Colt";
    public int damage = 10;
    public float range = 100;

    public float shootRate = 10f; //一秒钟可以打多少子弹，如果小于等于0则为单发
    public float shootCoolDownTime = 0.75f; //当为单发模式时，冷却时间
    public float recoilForce = 2f; //后坐力

    public int maxBullets = 30;
    public int bullets = 30;
    public float reloadTime = 2f;

    [HideInInspector]
    public bool isReloading = false;

    public GameObject graphics;
}
