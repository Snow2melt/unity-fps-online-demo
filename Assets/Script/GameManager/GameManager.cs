using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    public static GameManager Singleton;

    private static Dictionary<string, Player> players = new Dictionary<string, Player>();//用来存储名字和玩家关系，每个窗口都有


    private void Awake()
    {
        Singleton = this;
        Application.targetFrameRate = 144;
        QualitySettings.vSyncCount = 0; // 关闭VSync，不然会锁帧
    }

    public void RegisterPlayer(string name, Player player)
    {
        player.transform.name = name;
        players.Add(name, player);
    }

    public void UnRegisterPlayer(string name)
    {
        players.Remove(name);
    }

    public Player GetPlayer(string name)
    {
        return players[name];
    }

    /*private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(200f, 200f, 200f, 400f));
        GUILayout.BeginVertical();
        GUI.color = Color.red;

        foreach (string name in players.Keys)
        {
            Player player = GetPlayer(name);
            GUILayout.Label(name + " - " + player.GetHealth());
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }*/
    // GameManager.cs 里加
    public static IReadOnlyDictionary<string, Player> GetPlayers()
    {
        return players;
    }
    public bool TryGetLocalPlayerName(out string name)
    {
        name = null;
        foreach (var kv in players)
        {
            if (kv.Value != null && kv.Value.IsLocalPlayer)
            {
                name = kv.Key;
                return true;
            }
        }
        return false;
    }
}
