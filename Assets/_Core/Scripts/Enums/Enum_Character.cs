public enum RegionType
{
    NONE = -1,

    WEI,    // 위
    SHU,    // 촉
    WU,     // 오
    ETC,     // 중립

    MAX,

    Historical,
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
    Attack_Move,
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

public enum CharacterName
{
    LiuBei,                 // 유비
    GuanYu,                 // 관우
    ZhangFei,               // 장비
    ZhaYun,                 // 조운
    HuangZhong,             // 황충
    ZhugeLiang,             // 제갈량
    LuBu,                   // 여포
    CaoCao,                 // 조조
    CaoRen,                 // 조인
    XiahouDun,              // 하후돈
    ZhangLiao,              // 장료
    XiahouYuan,             // 하후연
    XunYu,                  // 순욱
    SunJian,                // 손견  
    SunQuan,                // 손권  
    HuangGai,               // 황개  
    SunCe,                  // 손책  
    TaishiCi,               // 태사자
    HanDang,                // 한당  
    ZhouYu,                 // 주유  

    YuanShao,               // 원소
    YanLiang,               // 안량
    WenChou,                // 문추
    SongXian,               // 송헌
    WeiXU,                  // 위속
    XuHuang,	            // 서황

    Etc
}