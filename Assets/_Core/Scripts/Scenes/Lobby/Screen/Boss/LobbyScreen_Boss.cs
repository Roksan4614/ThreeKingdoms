using UnityEngine;

public class LobbyScreen_Boss : LobbyScreen_Base
{
    public override void Open(LobbyScreenType _prevScreen)
    {
        base.Open(_prevScreen);

        CameraManager.instance.SetAddPosY(-2, 20);
    }

    public override void Close(bool _isTween = true)
    {
        base.Close(_isTween);

        CameraManager.instance.SetAddPosY(0, 20);
    }
}
