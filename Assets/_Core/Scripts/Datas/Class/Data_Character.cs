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

    public float duration_respawn;

    public void SetDefault() {

        attackPower = 100;

        attackSpeed = 1;
        moveSpeed = 10;

        health = healthMax = 2000;

        duration_respawn = 15;
    }
}
