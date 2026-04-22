public enum RegionType
{
    NONE = -1,

    Wei,    // 위
    Shu,    // 촉
    Wu,     // 오
    Etc,     // 중립

    MAX
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
    Walk_Back,
    Dash,
    Dash_Back,

    Attack,
    Skill,

    Die_1,
    Die_2,

    MAX
}

public enum GradeType
{
    NONE = -1,

    Normal,     //일반
    Elite,      //정예, 어려움
    General,    //명장, 지옥
    Hero,       //영웅, 심연
    Legend,     //전설

    MAX
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
    Chaser,
    // 궁장
    Archer,
    // 책사
    Strategist,

    MAX
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

public enum CoreStatType
{
    NONE = -1,

    Leadership,
    Strength,
    Intellect,
    Politics,
    Charisma,

    MAX
}

public enum BattleStatType
{
    NONE = -1,

    attack_power,
    defence,
    attack_speed,
    health_max,
    move_speed,
    life_steal,
    critical_rate,
    cooldown_rate,
    critical_damage,
    boss_damage,

    MAX
}