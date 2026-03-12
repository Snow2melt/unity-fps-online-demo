using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Singleton;

    private static Dictionary<string, Player> players = new Dictionary<string, Player>();

    private void Awake()
    {
        Singleton = this;
        Application.targetFrameRate = 144;
        QualitySettings.vSyncCount = 0;
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