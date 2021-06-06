﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR.Haptics;
using UnityEngine.UI;

/**
 * This class handles the initialization of our Lego Smash game
 */
public class SmashGameR : MiniGame
{
    public OurMinifigController player1;
    public OurMinifigController player2;
    public OurMinifigController player3;
    public OurMinifigController player4;
    private OurMinifigController[] players;
    public int gameDuration;

    public Countdown countdown;

    private float startTime;
    private float timeLeft;
    private int place = 4;
    private bool endCountdownCalled = false;

    public override string getDisplayName()
    {
        return "LEGO Smash";
    }
    public override string getSceneName()
    {
        return "Scene1Thilo";
    }

    public override MiniGameType getMiniGameType()
    {
        return MiniGameType.freeForAll;
    }

    private void Start()
    {
        players = new OurMinifigController[] {player1, player2, player3, player4};
        
        //Create list of player inputs from the players in the scene
        var playerInputs = new List<PlayerInput> { player1.GetComponent<PlayerInput>(), player2.GetComponent<PlayerInput>(), 
            player3.GetComponent<PlayerInput>(), player4.GetComponent<PlayerInput>() };

        //This assigns the player input in the order they were given in the array
        InputManager.Instance.AssignPlayerInput(playerInputs);

        startTime = Time.time + gameDuration + 3;
        countdown.StartCountDown(1);

    }

    void Update()
    {
        timeLeft = startTime - Time.time;
        if (timeLeft > gameDuration)
        {
            foreach (OurMinifigController p in players)
                p.SetInputEnabled(false);
        }
        else if (timeLeft > 0)
        {
            foreach (OurMinifigController p in players)
                p.SetInputEnabled(true);
        }
        else
        {
            foreach (OurMinifigController p in players)
                p.SetInputEnabled(false);
        }

        if (3 > timeLeft && timeLeft > 0 && !endCountdownCalled)
        {
            endCountdownCalled = true;
            countdown.StartCountDown(2);
        }

        //Check if players died to determine place
        foreach (OurMinifigController p in players)
        {
            if (p.died && !p.noticedDeath && place>=1)
            {
                p.noticedDeath = true;
                p.place = place;
                place -= 1;
            }
        }

        //Time is up if only 1 player is left
        if (place == 1)
        {
            startTime = Time.time; //-> timeLeft = 0
            place -= 1;
            countdown.StartCountDown(0);
            
        }

        Debug.Log(timeLeft);

        if (timeLeft < -2)
        {
            foreach (OurMinifigController p in players)
            {
                switch (p.place)
                {
                    case 1:
                        p.PlaySpecialAnimation(OurMinifigController.SpecialAnimation.Dance);
                        break;
                    case 2:
                        p.PlaySpecialAnimation(OurMinifigController.SpecialAnimation.Flexing);
                        break;
                    case 3:
                        p.PlaySpecialAnimation(OurMinifigController.SpecialAnimation.Wave);
                        break;
                    case 4:
                        p.PlaySpecialAnimation(OurMinifigController.SpecialAnimation.IdleHeavy);
                        break;
                }
            }
        }


        if (timeLeft < -10)
        {
            //Create array of positions with player ids, this also works in case there are multiple players in one position
            int[] first = { 0 };
            int[] second = { 1 };
            int[] third = { 2 };
            int[] fourth = { 3 };

            int id = player1.GetInstanceID();

            foreach (OurMinifigController p in players)
            {
                switch (p.place)
                {
                    case 1:
                        first.Append(p.GetInstanceID());
                        break;
                    case 2:
                        second.Append(p.GetInstanceID());
                        break;
                    case 3:
                        third.Append(p.GetInstanceID());
                        break;
                    case 4:
                        fourth.Append(p.GetInstanceID());
                        break;
                }
            }

            //Note this is still work in progress, but ideally you will use it like this
            MiniGameFinished(firstPlace: first, secondPlace: second, thirdPlace: third, fourthPlace: fourth);
        }

    }
}