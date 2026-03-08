using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField]
    private Behaviour[] componentsToDisable;
    private Camera sceneCamera;

    /*private void Awake()
    {
        sceneCamera = GetComponentInParent<Camera>();
    }*/

    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsLocalPlayer)
        {
            SetLayerMaskForAllChildren(transform, LayerMask.NameToLayer("Remote Player"));
            DisableComponents();
        }
        else
        {
            PlayerUI.Singleton.setPlayer(GetComponent<Player>());
            SetLayerMaskForAllChildren(transform, LayerMask.NameToLayer("Player"));
            sceneCamera = Camera.main;
            if (sceneCamera != null)
            {
                sceneCamera.gameObject.SetActive(false);
            }
        }

        string name = "Player" + GetComponent<NetworkObject>().NetworkObjectId.ToString();
        Player player = GetComponent<Player>();

        player.Setup();
        GameManager.Singleton.RegisterPlayer(name, player);

        //RegisterPlayer();
        //SetPlayerName();
    }

    private void SetLayerMaskForAllChildren(Transform transform, LayerMask layerMask)
    {
        transform.gameObject.layer = layerMask;
        for (int i = 0; i < transform.childCount; i ++ )
        {
            SetLayerMaskForAllChildren(transform.GetChild(i), layerMask);
        }
    }

    /*private void RegisterPlayer()
    {
        string name = "Player" + GetComponent<NetworkObject>().NetworkObjectId.ToString();
        Player player = GetComponent<Player>();
        GameManager.Singleton.RegisterPlayer(name, player);
    }*/

    private void SetPlayerName()
    {
        transform.name = "Player" + GetComponent<NetworkObject>().NetworkObjectId;
    }

    private void DisableComponents()
    {
        for (int i = 0; i < componentsToDisable.Length; i ++)
        {
            componentsToDisable[i].enabled = false;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (sceneCamera != null)
        {
            sceneCamera.gameObject.SetActive(true);
        }
        GameManager.Singleton.UnRegisterPlayer(transform.name);
    }
    /*private void OnDisable()
    {
        
    }*/
}
