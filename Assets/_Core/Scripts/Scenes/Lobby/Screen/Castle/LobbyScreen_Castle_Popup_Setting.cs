using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Castle_Popup_Setting : MonoBehaviour
{
    private void Awake()
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(() => Close());
    }

    public async UniTask OpenAsync(CastleObjectType _type, CancellationToken _cancelToken)
    {
        gameObject.SetActive(true);

        await UniTask.WaitUntil(() => gameObject.activeSelf == false, cancellationToken: _cancelToken);
    }
    public void Close(StatusType _result = StatusType.Cancel)
    {
        gameObject.SetActive(false);
    }
}
