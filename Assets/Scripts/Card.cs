using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerDownHandler
{
    public TextMeshPro topText;
    public TextMeshPro botText;
    public TextMeshPro rightText;
    public TextMeshPro leftText;
    public int top;
    public int right;
    public int bot;
    public int left;
    public int player;
    public bool played;
    public bool selected = false;
    public GameObject back;

    public void Initialize(int top, int right, int bot, int left)
    {
        this.top = top;
        this.right = right;
        this.bot = bot;
        this.left = left;
        topText.text = top.ToString() != "10" ? top.ToString() : "A";
        botText.text = bot.ToString() != "10" ? bot.ToString() : "A";
        rightText.text = right.ToString() != "10" ? right.ToString() : "A";
        leftText.text = left.ToString() != "10" ? left.ToString() : "A";
        this.played = false;
        
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        GameManager instance = GameManager.GetInstance();
        if (instance.currentPlayer != this.player || (instance.vsAI && instance.currentPlayer == 1))
            return;
        instance.SelectCard(this);
    }


}
