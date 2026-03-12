using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;

public class ScoreboardUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI text;

    private float nextRefreshTime = 0f;

    private const float RefreshInterval = 0.2f;

    void Start()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    void Update()
    {
        if (panel == null || text == null) return;

        bool show = Input.GetKey(KeyCode.Tab);
        bool wasShowing = panel.activeSelf;

        if (wasShowing != show)
        {
            panel.SetActive(show);

            if (show)
            {
                nextRefreshTime = 0f;
                RefreshScoreboard();
            }
        }

        if (!show) return;

        if (Time.time < nextRefreshTime) return;
        nextRefreshTime = Time.time + RefreshInterval;

        RefreshScoreboard();
    }

    private void RefreshScoreboard()
    {
        string localName = null;
        if (GameManager.Singleton != null && GameManager.Singleton.TryGetLocalPlayerName(out var n))
            localName = n;

        var players = GameManager.GetPlayers()
            .Where(kv => kv.Value != null && !kv.Value.IsBot)
            .Select(kv => new
            {
                Name = kv.Key,
                K = kv.Value.GetKills(),
                D = kv.Value.GetDeaths()
            })
            .OrderByDescending(x => x.K)
            .ThenBy(x => x.D)
            .ThenBy(x => x.Name)
            .ToList();

        StringBuilder sb = new StringBuilder();

        const int nameMaxLen = 12;
        const int kPos = 250;
        const int dPos = 300;
        const string line = "©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤";

        sb.AppendLine("<size=36><b>SCOREBOARD</b></size>");
        sb.AppendLine("<color=#FFFFFF55>" + line + "</color>");
        sb.AppendLine($"<size=22><color=#FFFFFFCC><b>NAME</b><pos={kPos}><b>K</b><pos={dPos}><b>D</b></color></size>");
        sb.AppendLine("<color=#FFFFFF33>" + line + "</color>");

        foreach (var p in players)
        {
            string displayName = p.Name;
            if (displayName.Length > nameMaxLen)
                displayName = displayName.Substring(0, nameMaxLen) + "...";

            bool isMe = !string.IsNullOrEmpty(localName) && p.Name == localName;

            if (isMe)
            {
                displayName += " [You]";
                sb.AppendLine(
                    $"<size=20><color=#FFD86A><b>{displayName}</b></color><pos={kPos}><color=#FFD86A><b>{p.K}</b></color><pos={dPos}><color=#FFD86A><b>{p.D}</b></color></size>"
                );
            }
            else
            {
                sb.AppendLine(
                    $"<size=20><color=#F2F2F2>{displayName}</color><pos={kPos}><color=#F2F2F2>{p.K}</color><pos={dPos}><color=#F2F2F2>{p.D}</color></size>"
                );
            }
        }

        text.text = sb.ToString();
    }
}