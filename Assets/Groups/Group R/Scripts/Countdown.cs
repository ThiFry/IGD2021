using UnityEngine;
using UnityEngine.UI;

public class Countdown : MonoBehaviour
{
    public Text countdownText;
    int countdown;
    float timeAtStart;
    string txt_after_cntdwn;

    public AudioSource audioSource;
    public AudioClip countdownAudioClip;

    // Update is called once per frame
    void Update()
    {
        countdown = (int) (timeAtStart - Time.time);

        switch (countdown)
        {
            case 4:
                countdownText.text = "3";
                break;
            case 3:
                countdownText.text = "2";
                break;
            case 2:
                countdownText.text = "1";
                break;
            case 1:
                countdownText.text = txt_after_cntdwn;
                break;
            default:
                countdownText.text = "";
                break;
        }
    }

    public void StartCountDown(int type)
    {
        timeAtStart = Time.time + 4.999f;
        if (type == 1){
            //Start of game
            txt_after_cntdwn = "Smash!";
            audioSource.PlayOneShot(countdownAudioClip);
        }
        else if (type == 2)
        {
            //Maximum game time has been reacheds
            txt_after_cntdwn = "Time's Up!";
            audioSource.PlayOneShot(countdownAudioClip);
        }
        else
        {
            //All player died
            txt_after_cntdwn = "Game Over!";
            timeAtStart = Time.time + 1.999f;
        }
    }
}
