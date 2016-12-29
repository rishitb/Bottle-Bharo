using UnityEngine;
using System.Collections;

/// <summary>
/// Manages the time module for the game and exposes the required functions
/// </summary>
public class TimeManager : MonoBehaviour {

    public TimeManager _instance;

    public bool isPaused;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else {
            _instance = this;
            DontDestroyOnLoad(this);
        }

        isPaused = true;
    }

    //Called once per frame
    void Update()
    {
        if (!isPaused)
        {
            timeLeft -= 1f*Time.deltaTime;
            Debug.Log("<color=blue>Time Left : " + timeLeft + "</color>");
        }

        //if (Input.GetKeyDown(KeyCode.S))
        //    StartTimer(30f);

        //if (Input.GetKeyDown(KeyCode.P))
        //    PauseTimer();

        //if (Input.GetKeyDown(KeyCode.R))
        //    ResumeTimer();

        //if (Input.GetKeyDown(KeyCode.T))
        //    StopTimer();

        //if (Input.GetKeyDown(KeyCode.A))
        //    AddTime(5f);

        //if (Input.GetKeyDown(KeyCode.D))
        //    DeductTime(2f);
    }

    public float timeLeft;

    /// <summary>
    /// Adds x seconds to the amount of time remaining
    /// </summary>
    /// <param name="secondsToAdd">Number of seconds to be added</param>
    public void AddTime(float secondsToAdd)
    {
        timeLeft += secondsToAdd;
    }

    /// <summary>
    /// Reduces the amount of time remaining by x seconds
    /// </summary>
    /// <param name="secondsToDeduct">Number of seconds to deduct</param>
    public void DeductTime(float secondsToDeduct)
    {
        timeLeft -= secondsToDeduct;
    }

    /// <summary>
    /// Pauses the timer
    /// This pauses the timer only and doesnt affect the timescale of the game, hence separate from PauseGame
    /// </summary>
    public void PauseTimer()
    {
        isPaused = true;
    }

    /// <summary>
    /// Resumes the timer
    /// This resumes the timer only and doesnt affect the timescale of the game, hence separate from ResumeGame
    /// </summary>
    public void ResumeTimer()
    {
        isPaused = false;
    }

    /// <summary>
    /// This kicks off the timer 
    /// </summary>
    /// <param name="initTime">Initialization value for the time left</param>
    public void StartTimer(float initTime)
    {
        timeLeft = initTime;
        isPaused = false;
    }

    /// <summary>
    /// Stops the timer and sets the value of time left back to 0
    /// </summary>
    public void StopTimer()
    {
        isPaused = true;
        timeLeft = 0f;
    }

    /// <summary>
    /// Returns how much time is left
    /// </summary>
    /// <returns></returns>
    public float GetTimeLeft()
    {
        //TODO return time in seconds ? or upto 2 decimals
        return timeLeft;
    }
}
