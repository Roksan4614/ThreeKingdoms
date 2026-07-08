using UnityEngine;

public class Palette
{
    static Palette m_instance;

    public static Palette instance
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

    public static Color GetGradeOutline(GradeType _grade)
        => instance.m_data.Get($"icon_outline_grade_{_grade.ToString().ToLower()}");
    public static string GetHexa_GradeOutline(GradeType _grade)
        => ColorUtility.ToHtmlStringRGB(GetGradeOutline(_grade));

    public static Color GetGradeText(GradeType _grade)
        => instance.m_data.Get($"icon_outline_grade_{_grade.ToString().ToLower()}");
    public static string GetHexa_GradeText(GradeType _grade)
        => ColorUtility.ToHtmlStringRGB(GetGradeText(_grade));

    public static Color Get(PaletteColorType _colorType)
        => Get(_colorType.ToString());

    public static Color Get(string _colorString)
        => instance.m_data.Get(_colorString);

    public static string GetHexadecimal(PaletteColorType _colorType)
        => instance.m_data.GetHexadecimal(_colorType);

    public static string htmlString_Up => instance.m_data.GetHexadecimal(PaletteColorType.txt_up);
    public static string htmlString_Down => instance.m_data.GetHexadecimal(PaletteColorType.txt_down);
}
