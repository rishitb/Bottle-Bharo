using UnityEngine;
using System.Collections;

/// <summary>
/// Manages the score module throughout the game and exposes corresponsding required function
/// </summary>
public class ScoreManager : MonoBehaviour {

    public static ScoreManager _instance;

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
    }

    public int currentScore;
    public int highScore;

    /// <summary>
    /// Increments score by 1 unit 
    /// </summary>
    public void IncrementScore(int toAdd=1)
    {
        currentScore += toAdd;
        if (currentScore > highScore)
            UpdateHighScore();
    }

    /// <summary>
    /// Resets the game score back to 0
    /// </summary>
    public void ResetScore()
    {
        currentScore= 0;
    }

    /// <summary>
    /// Updates the value of the high score
    /// Cannot be called externally
    /// </summary>
    private void UpdateHighScore()
    {
        highScore = currentScore;
        //TODO make this server based
        PlayerPrefs.SetInt("highscore", highScore);
    }

    /// <summary>
    /// Returns the value for the current score
    /// </summary>
    /// <returns>Current score</returns>
    public int GetScore()
    {
        return currentScore;
    }

    /// <summary>
    /// Returns the value for the high score
    /// </summary>
    /// <returns>High score</returns>
    public int GetHighScore()
    {
        highScore = PlayerPrefs.GetInt("highscore");
        //TODO make this server based
        return highScore;
    }
}
