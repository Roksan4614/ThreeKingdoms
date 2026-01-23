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

    public void SetDefault() {

        attackPower = 100;

        moveSpeed = 10;
        health = healthMax = 1000;
    }
}
