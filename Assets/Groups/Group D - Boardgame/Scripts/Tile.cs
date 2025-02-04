﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public enum TileType {
        GAIN_COINS,
        LOSE_COINS,
        MASTER_HAND,
        RANDOM_EVENT,
        START,
    }

    public TileType type;
    public Tile left;
    public Tile right;
    public Tile up;
    public Tile down;
    bool trapPresent;
    int trapOwner;

    private bool goldenBrickPresent;
    public bool itemShopPresent;

    public Vector3 getPosition() {
        return this.transform.position;
    }

    public bool hasItemShop() {
        return itemShopPresent;
    }

    public bool hasGoldenBrick() {
        return goldenBrickPresent;
    }

    public void setHasGoldenBrick(bool newValue) {
        goldenBrickPresent = newValue;
    }

    public bool hasTrap(){
        return trapPresent;
    }

    public void setTrap(bool newValue, int player=-1)
    {
        trapPresent = newValue;
        trapOwner = player;
    }

    public int getTrapOwner()
    {
        return trapOwner;
    }
}
