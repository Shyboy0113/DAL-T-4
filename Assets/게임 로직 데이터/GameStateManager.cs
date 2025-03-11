using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public bool IsGameOver { get; private set; } = false;

    private void Start()
    {
        ResetGame();
    }

    public void SetGameOver()
    {
        IsGameOver = true;
    }

    public void ResetGame()
    {
        IsGameOver = false;
    }

}
