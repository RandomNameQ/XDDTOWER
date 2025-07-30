using System;
using System.Collections.Generic;
using System.Linq;
using Core.Card;
using Mono.Cecil;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.StateUpdaters;
using UnityEngine;
using UnityEngine.InputSystem.Composites;
using UnityEngine.UI;

public class ShopBase : MonoBehaviour
{
    public ShopperSO shopperData;
    public Image shoperAvatar;

    // в какой обьект будемт втавлять итемы
    public GameObject placeForCards;

    public ShopperInit shopper;

    public GameObject selectedCard;
    public List<GameObject> itemSlots = new();

    public GameObject cardBodyPrefab;

    public GameObject shopPanel;


    private void Start()
    {
        InitShopper();
    }

    [Button]
    public void InitShopper()
    {

        shopper = new ShopperInit(this, shoperAvatar, placeForCards);
        shopper.countCards = 3;
        shopper.InitShopperData();
    }




    [Serializable]
    public class ShopperInit
    {
        private ShopBase shopBase;
        private Image avatar;
        public int countCards;
        public List<CardData> choosenCards = new();

        public ShopperInit(ShopBase shopBase, Image avatar, GameObject shopObject)
        {
            this.shopBase = shopBase;
            this.avatar = avatar;
        }
        public void InitShopperData()
        {
            avatar.sprite = shopBase.shopperData.image;
            GetRandomCard();
            InstallCartDataInShop();
        }

        public void GetRandomCard()
        {
            for (int i = 0; i < countCards; i++)
            {
                if (RandomManager.Q == null)
                {
                    Debug.LogError("RandomManager is not initialized");
                    return;
                }
                CardData card = shopBase.shopperData.cards[RandomManager.Q.Range(0, shopBase.shopperData.cards.Count)];
                if (!choosenCards.Contains(card))
                {
                    choosenCards.Add(card);
                }
                else
                {
                    i--;
                }
            }
        }



        public void InstallCartDataInShop()
        {
            GameObject buttonForBuy = null;
            // удаляем все дочерние обьекты
            foreach (Transform child in shopBase.placeForCards.transform)
            {
                if (child.GetComponent<LayoutElement>() == null)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    buttonForBuy = child.gameObject;
                }
            }
            shopBase.itemSlots.Clear();

            foreach (var card in choosenCards)
            {
                GameObject cardObject = Instantiate(new GameObject(), shopBase.placeForCards.transform);
                cardObject.AddComponent<Image>();
                cardObject.AddComponent<ItemSlot_Shop>().cardData = card;
                cardObject.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 99999999999);
                cardObject.GetComponent<Image>().sprite = card.image;
                shopBase.itemSlots.Add(cardObject);
            }
            buttonForBuy.transform.SetAsLastSibling();
        }
    }




    public void TryBuyCard()
    {
        // потом добавить проверку на ресурсы, проверка на есть ли место в инвентаре, и сделать уфнкцию для добавления карты в сводобное место инвантаря
        if (selectedCard != null)
        {
            var shopCardData = selectedCard.GetComponent<ItemSlot_Shop>().cardData;

            List<BoardGrid> boards = FindObjectsByType<BoardGrid>(FindObjectsSortMode.None).ToList();
            BoardGrid playerInventory = boards.Where<BoardGrid>(x => x.boardType == BoardType.PlayerInventory).FirstOrDefault();
            BoardGrid gameBoard = boards.Where<BoardGrid>(x => x.boardType == BoardType.GameBoard).FirstOrDefault();

            List<Vector3Int> freePositions = playerInventory.FindFreePositions(shopCardData.sizeX, shopCardData.sizeZ);
            if (freePositions.Count > 0)
            {
                var card = CreateCardBody();
                playerInventory.TryPlaceCardAtPositions(card, freePositions);
                return;
            }

            freePositions = gameBoard.FindFreePositions(shopCardData.sizeX, shopCardData.sizeZ);
            if (freePositions.Count > 0)
            {
                var card = CreateCardBody();

                gameBoard.TryPlaceCardAtPositions(card, freePositions);
            }

        }
    }

    public GameObject CreateCardBody()
    {
        var body = Instantiate(cardBodyPrefab).GetComponent<PawnCreature>();
        var shopCardData = selectedCard.GetComponent<ItemSlot_Shop>().cardData;

        body.cardData = shopCardData;
        body.name = shopCardData.name;

        body.Init();

        return body.gameObject;
    }


    public void SelectCardInShop(GameObject card)
    {
        selectedCard = card;
        foreach (var item in itemSlots)
        {
            item.GetComponent<ItemSlot_Shop>().image.color = item == selectedCard ? Color.green : Color.white;
        }
    }

    private void OnEnable()
    {
        GlobalEvent.OnInteractShop += InteractShop;
    }
    private void OnDisable()
    {
        GlobalEvent.OnInteractShop -= InteractShop;
    }

    public void InteractShop()
    {
        shopPanel.SetActive(!shopPanel.activeSelf);
    }
}
