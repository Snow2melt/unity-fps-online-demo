using UnityEngine;

public class RangeControlButton : MonoBehaviour
{
    public enum ButtonType
    {
        Start,
        Stop
    }

    [SerializeField] private ButtonType buttonType;

    public void Trigger()
    {
        if (RangeSessionManager.Instance == null) return;

        if (buttonType == ButtonType.Start)
        {
            RangeSessionManager.Instance.StartSessionByServer();
        }
        else
        {
            RangeSessionManager.Instance.StopSessionByServer();
        }
    }
}