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
    Dash,

    Attack,

    Die_1,
    Die_2,

    MAX
}

public enum GradeType
{
    NONE = -1,

    Normal,     //일반
    Elite,      //정예, 어려움
    General,    //장군, 지옥
    Hero,       //영웅, 심연
    Legend,     //전설
}

public enum HeroClassType
{
    NONE = -1,

    // 지휘관
    Commander,
    // 용장
    Champion,
    // 선봉장
    Vanguard,
    // 추격자
    Sentinel,
    // 궁장
    Archer,
    // 책사
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