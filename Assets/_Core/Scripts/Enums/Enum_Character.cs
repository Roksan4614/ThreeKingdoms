public enum RegionType
{
    Wei, Shu, Wu, Etc
}

public enum FactionType
{
    NONE,

    Alliance,
    Enemy,

    ETC,
}

public enum CharacterAnimType
{
    NONE = -1,

    Idle,
    Walk,

    Attack,

    Die_1,
    Die_2,

    MAX
}

public enum HeroGradeType
{
    NONE = -1,

    Normal,
    Elite,
    General,
    Hero,
    Legend,
}

public enum HeroClassType
{
    NONE = -1,

    // ÁöÈÖ°ü
    Commander,
    // ¿ëÀå
    Champion,
    // ¼±ºÀÀå
    Vanguard,
    // Ãß°ÝÀÚ
    Sentinel,
    // ±ÃÀå
    Archer,
    // Ã¥»ç
    Strategist,
}

public enum TeamPositionType
{
    NONE = -1,

    Front,
    Top,
    Bottom,
    Back,

    MAX
}