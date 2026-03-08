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

        if (panel.activeSelf != show)
            panel.SetActive(show);

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
            .Where(kv => kv.Value != null)
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

        sb.AppendLine("<size=38><b>SCOREBOARD</b></size>");
        sb.AppendLine("<color=#FFFFFF66>©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤</color>");
        sb.AppendLine();
        sb.AppendLine("<color=#FFFFFFAA><b>NAME</b><pos=240><b>K</b><pos=300><b>D</b></color>");
        sb.AppendLine("<color=#FFFFFF66>©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤</color>");
        sb.AppendLine();

        foreach (var p in players)
        {
            string name = p.Name;
            if (name.Length > 16)
                name = name.Substring(0, 16);

            bool isMe = !string.IsNullOrEmpty(localName) && p.Name == localName;

            if (isMe)
            {
                sb.AppendLine(
                    $"<color=#FFD86A><b>{name}</b></color><pos=240><color=#FFD86A><b>{p.K}</b></color><pos=300><color=#FFD86A><b>{p.D}</b></color>"
                );
            }
            else
            {
                sb.AppendLine($"{name}<pos=240>{p.K}<pos=300>{p.D}");
            }
        }

        text.text = sb.ToString();
    }
}