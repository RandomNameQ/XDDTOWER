using System.Collections.Generic;
using Core.Card;
using UnityEngine;

[CreateAssetMenu(fileName = "Shopper", menuName = "ScriptableObjects/Shopper")]
public class ShopperSO : ScriptableObject
{
    public new string name;
    public string description;
    public Sprite image;
    public List<CardData> cards = new();
}
