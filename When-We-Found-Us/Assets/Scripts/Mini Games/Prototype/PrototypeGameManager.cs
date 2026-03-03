using UnityEngine;
using System.Collections;

public class PrototypeGameManager : MiniGameManager
{
    void Start()
    {
        StartCoroutine(WaitAndWin());
    }

    IEnumerator WaitAndWin()
    {
        yield return new WaitForSeconds(10f);
        WinGame();
    }
}
