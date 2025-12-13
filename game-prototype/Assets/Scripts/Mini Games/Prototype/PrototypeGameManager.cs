using UnityEngine;
using System.Collections;

public class PrototypeGameManager : MiniGameManager
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            WinGame();
        }
    }
}
