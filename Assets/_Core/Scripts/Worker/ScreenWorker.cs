using UnityEngine;

public static class ScreenWorker
{
    static ScreenOrientation m_curOrientationType = ScreenOrientation.PortraitUpsideDown;
    public static void SetDisplayMode(ScreenOrientation _orientation)
    {
        if (_orientation == ScreenOrientation.Portrait)
            _orientation = ScreenOrientation.PortraitUpsideDown;
        else if (_orientation == ScreenOrientation.LandscapeRight)
            _orientation = ScreenOrientation.LandscapeLeft;

        if (m_curOrientationType == _orientation)
            return;

        m_curOrientationType = _orientation;

        Screen.orientation = ScreenOrientation.AutoRotation;

        bool isLandscape = _orientation == ScreenOrientation.LandscapeLeft ||
            _orientation == ScreenOrientation.LandscapeRight;

        Screen.autorotateToLandscapeLeft = isLandscape;
        Screen.autorotateToLandscapeRight = isLandscape;

        Screen.autorotateToPortrait = isLandscape == false;
        Screen.autorotateToPortraitUpsideDown = isLandscape == false;

        Signal.instance.ChangeDisplayMode.Emit(isLandscape);
    }

}
