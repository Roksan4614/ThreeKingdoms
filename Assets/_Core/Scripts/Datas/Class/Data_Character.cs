using System;
using UnityEngine;

[Serializable]
public struct Data_Character
{
    public CharacterClassType classType;

    public int attackPower;

    public int healthMax;
    public int health;

    public float moveSpeed;
    public float attackSpeed;

    public float cooltime_skill;

    public float duration_respawn; //사망 후 부활까지 시간

    public float percent_startCooltime; //챕터 시작하면 쿨타임 몇퍼부터 시작할지 여부

    public void SetDefault() {

        attackPower = 100;

        attackSpeed = 1;
        moveSpeed = 10;

        health = healthMax = 2000;
        cooltime_skill = 10f;

        duration_respawn = 15;
        percent_startCooltime = 0.8f;
    }
}
