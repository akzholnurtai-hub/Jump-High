using UnityEngine;
using System.Collections.Generic;

public class SurvivalTimer : MonoBehaviour
{
    public Animator gateAnimator;
    public List<GameObject> coins = new List<GameObject>();

    bool opened;

    void Start()
    {

        if (coins.Count == 0)
        {
            foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("Coin"))
                    coins.Add(go);
            }
        }

        Debug.Log("Сахнадағы монеталар саны: " + coins.Count);
    }

    void Update()
    {
        if (opened || coins.Count == 0) return;

        int left = 0;
        foreach (GameObject coin in coins)
        {
            if (coin != null && coin.activeInHierarchy)
                left++;
        }

        if (left == 0)
        {
            opened = true;
            Debug.Log("Барлық монета жиналды! Есік ашылды.");

            if (gateAnimator != null)
                gateAnimator.SetBool("Open", true);
            else
                Debug.LogWarning("Gate Animator берілмеген! Inspector-да есікті салыңыз.");
        }
    }
}