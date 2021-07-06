using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public Text one;
    public Text two;
    public Text three;
    public Text four;
    public Vector3 firstPos;
    public Vector3 secondPos;
    public Vector3 thirdPos;
    public Vector3 fourthPos;
    public AIAgentR AI1;
    public AIAgentR AI2;
    public AIAgentR AI3;
    public AIAgentR AI4;

    public Countdown countdown;
    public AudioSource audioSource;
    public AudioClip endJingle;
    public int gameDuration;

    private float endTime;
    private float timeLeft;
    private int place = 4;
    private bool startCountdownCalled = false;
    private bool startedGame = false;
    private bool endCountdownCalled = false;
    private bool playedEndJingle = false;

    public override string getDisplayName()
    {
        return "LEGO Smash";
    }
    public override string getSceneName()
    {
        return "SceneR";
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

        AI1.gameObject.SetActive(PlayerPrefs.GetString("Player1_AI").Equals("True"));
        AI1.SetId(1);
        AI2.gameObject.SetActive(false);// PlayerPrefs.GetString("Player2_AI").Equals("True"));
        AI2.SetId(2);
        AI3.gameObject.SetActive(false);// PlayerPrefs.GetString("Player3_AI").Equals("True"));
        AI3.SetId(3);
        AI4.gameObject.SetActive(false);// PlayerPrefs.GetString("Player4_AI").Equals("True"));
        AI4.SetId(4);

        endTime = Time.time + gameDuration + 3;
        countdown.StartCountDown(1);

    }

    void Update()
    {
        timeLeft = endTime - Time.time;
        if (timeLeft > gameDuration && !startCountdownCalled)
        {
            startCountdownCalled = true;
            foreach (OurMinifigController p in players)
                p.SetInputEnabled(false);
        }
        if (timeLeft > 0 && timeLeft < gameDuration && !startedGame)
        {
            startedGame = true;
            foreach (OurMinifigController p in players)
                p.SetInputEnabled(true);
            audioSource.Play();
        }
        if (timeLeft < 0)
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
            endTime = Time.time; //-> timeLeft = 0
            place -= 1;
            countdown.StartCountDown(0);
        }

        if (timeLeft < -1)
        {
            if (!playedEndJingle)
            {
                playedEndJingle = true;
                audioSource.Stop();
                audioSource.PlayOneShot(endJingle);
            }
            foreach (OurMinifigController p in players)
            {
                switch (p.place)
                {
                    case 1:
                        one.text = "1";
                        // The following distinction is needed if more than one player is first.
                        if (p == player1)
                        {
                            firstPos.z = 0f;
                        }
                        else if (p == player2)
                        {
                            firstPos.z = 0.9f;
                        }
                        else if (p == player3)
                        {
                            firstPos.z = 1.8f;
                        }
                        else
                        {
                            firstPos.z = 2.7f;
                        }
                        p.TeleportTo(firstPos);
                        p.PlaySpecialAnimation(OurMinifigController.SpecialAnimation.Flexing);
                        break;
                    case 2:
                        two.text = "2";
                        p.PlaySpecialAnimation(OurMinifigController.SpecialAnimation.HatSwap);
                        p.TeleportTo(secondPos);
                        break;
                    case 3:
                        three.text = "3";
                        p.PlaySpecialAnimation(OurMinifigController.SpecialAnimation.HatSwap2);
                        p.TeleportTo(thirdPos);
                        break;
                    case 4:
                        four.text = "4";
                        p.PlaySpecialAnimation(OurMinifigController.SpecialAnimation.LookingAround);
                        p.TeleportTo(fourthPos);
                        break;
                }
                p.transform.rotation = Quaternion.Euler(new Vector3(0, 90, 0));
                p.gameOver = true;
            }
        }


        if (timeLeft < -6)
        {
            //Create array of positions with player ids, this also works in case there are multiple players in one position
            int[] first = {};
            int[] second = {};
            int[] third = {};
            int[] fourth = {};

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

    public (int[],bool[]) getGameState(){
        int[] playerPlatforms = {player1.getPlatform(),
        player2.getPlatform(),
        player3.getPlatform(),
        player4.getPlatform()};

        bool[] playerDied = {player1.died,player2.died,player3.died,player4.died};

        return (playerPlatforms,playerDied);
    }

    public OurMinifigController getPlayer(int id){
        return players[id];
    }
}
