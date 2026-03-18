using UnityEngine;

public enum ColorType
{

}

public class Palette
{
    static Palette m_instance;

    public Palette instance
    {
        get
        {

            if (m_instance == null)
            {
                m_instance = new();
                m_instance.m_data = Resources.Load<ColorPalette>("Settings/ColorPalette");
            }

            return m_instance;
        }
    }

    ColorPalette m_data;
    public ColorPalette data => m_data;
}
