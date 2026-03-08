using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManangerUI : MonoBehaviour
{
    [SerializeField]
    private Button hostBtn;
    [SerializeField]
    private Button serverBtn;
    [SerializeField]
    private Button clientBtn;
    [SerializeField]
    private Button room1;
    [SerializeField]
    private Button room2;

    // Start is called before the first frame update
    void Start()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        var args = System.Environment.GetCommandLineArgs();

        ushort port = 7777;
        bool launchAsServer = false;

        for (int i = 0; i < args.Length; i ++ )
        {
            //if (args[i] == "-port")
            if (args[i] == "-port" && i + 1 < args.Length)
            {
                port = ushort.Parse(args[i + 1]);
            }
        }
        for (int i = 0; i < args.Length; i ++ )
        {
            if (args[i] == "-launch-as-server")
            {
                launchAsServer = true;
            }
        }

        if (launchAsServer)
        {
            transport.ConnectionData.Port = port;
            transport.ConnectionData.ServerListenAddress = "0.0.0.0";

            NetworkManager.Singleton.StartServer();
            Debug.Log($"Server started on port {port}");

            return;
        }

        if (Application.isBatchMode) return;

        room1.onClick.AddListener(() =>
        {
            transport.ConnectionData.Address = "114.55.135.130";
            transport.ConnectionData.Port = 7777;

            NetworkManager.Singleton.StartClient();
            DestroyAllButtons();
        });

        room2.onClick.AddListener(() =>
        {
            transport.ConnectionData.Address = "114.55.135.130";
            transport.ConnectionData.Port = 7778;

            NetworkManager.Singleton.StartClient();
            DestroyAllButtons();
        });

        hostBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            DestroyAllButtons();
        });
        serverBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
            DestroyAllButtons();
        });
        clientBtn.onClick.AddListener(() => 
        {
            NetworkManager.Singleton.StartClient();
            DestroyAllButtons();
        });

    }

    private void DestroyAllButtons()
    {
        //Destroy(hostBtn.gameObject);
        //Destroy(serverBtn.gameObject);
        //Destroy(clientBtn.gameObject);
        if (room1 != null) Destroy(room1.gameObject);
        if (room2 != null) Destroy(room2.gameObject);
    }
}
