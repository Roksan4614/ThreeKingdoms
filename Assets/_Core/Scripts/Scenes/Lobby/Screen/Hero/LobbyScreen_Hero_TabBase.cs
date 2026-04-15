using Cysharp.Threading.Tasks;
using UnityEngine;

public class LobbyScreen_Hero_TabBase : MonoBehaviour
{
    public LobbyScreen_Hero.HeroTabType tabType { get; protected set; }

    public virtual void Awake() { }

    public virtual bool IsCloseScreen() => true;

    public virtual async UniTask CloseAsync() => await UniTask.Yield();

}
