using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    public CanvasGroup m_victoryBanner;

    public UnityAction e_playerWinEvent;


    private void Awake()
    {
        instance = this;

        // hide the victory banner at the start
        m_victoryBanner.alpha = 0;
        m_victoryBanner.gameObject.SetActive(false);
    }

    void Start()
    {
        // register the player win event
        e_playerWinEvent += PlayerWin;
    }



    /// <summary>
    /// play the wining progress 
    /// </summary>
    public void PlayerWin()
    {
        StartCoroutine(ShowVictoryBanner());
    }

    /// <summary>
    /// Show the victory banner with fade in effect
    /// </summary>
    /// <returns></returns>
    IEnumerator ShowVictoryBanner()
    {
        m_victoryBanner.gameObject.SetActive(true);

        while (m_victoryBanner.alpha < 1)
        {
            m_victoryBanner.alpha += Time.deltaTime * 0.5f;
            yield return null;
        }

        yield return null;
    }
}
