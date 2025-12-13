using UnityEngine;
using System.Collections;

public class PrototypeGameManager : MiniGameManager
{
    public float delayAfterWin = 5.0f;
    private IEnumerator Start(){        
        yield return new WaitForSeconds(delayAfterWin);
        WinGame();
        }

}
