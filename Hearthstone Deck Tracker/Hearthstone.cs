﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hearthstone_Deck_Tracker
{
    public class Hearthstone
    {

        //dont like this solution, cant think of better atm
        public static bool HighlightCardsInHand;


        private static Dictionary<string, Card> _cardDb;
        public ObservableCollection<Card> EnemyCards;
        public int EnemyHandCount;
        public int OpponentDeckCount;
        public bool IsInMenu;
        public ObservableCollection<Card> PlayerDeck;
        public ObservableCollection<Card> PlayerDrawn; 
        public int PlayerHandCount;
        public string PlayingAgainst;
        public string PlayingAs;
        public bool OpponentHasCoin;
        public bool IsUsingPremade;

        public int[] OpponentHand { get; private set; }

        private readonly List<string> _invalidCardIds = new List<string>
            {
                "EX1_tk34",
                "EX1_tk29",
                "EX1_tk28",
                "EX1_tk11",
                "EX1_598",
                "NEW1_032",
                "NEW1_033",
                "NEW1_034",
                "NEW1_009",
                "CS2_052",
                "CS2_082",
                "CS2_051",
                "CS2_050",
                "CS2_152",
                "skele11",
                "skele21",
                "GAME",
                "DREAM",
                "NEW1_006",
            };

        public Hearthstone(string languageTag)
        {
            IsInMenu = true;
            PlayerDeck = new ObservableCollection<Card>();
            PlayerDrawn = new ObservableCollection<Card>();
            EnemyCards = new ObservableCollection<Card>();
            _cardDb = new Dictionary<string, Card>();
            OpponentHand = new int[10];
            for (int i = 0; i < 10; i++)
            {
                OpponentHand[i] = -1;
            }

            LoadCardDb(languageTag);
        }

        private void LoadCardDb(string languageTag)
        {
            try
            {
                var localizedCardNames = new Dictionary<string, string>();
                if (languageTag != "enUS")
                {
                    var file = string.Format("Files/cardsDB.{0}.json", languageTag);
                    if(File.Exists(file))
                    {
                        var localized = JObject.Parse(File.ReadAllText(file));
                        foreach (var cardType in localized)
                        {
                            if (cardType.Key != "Basic" && cardType.Key != "Expert" && cardType.Key != "Promotion" &&
                                cardType.Key != "Reward") continue;
                            foreach (var card in cardType.Value)
                            {
                                var tmp = JsonConvert.DeserializeObject<Card>(card.ToString());
                                localizedCardNames.Add(tmp.Id, tmp.Name);
                            }
                        }
                    }
                }


                //load engish db (needed for importing, etc)
                var fileEng = "Files/cardsDB.enUS.json";
                var tempDb = new Dictionary<string, Card>();
                if(File.Exists(fileEng))
                {
                    var obj = JObject.Parse(File.ReadAllText(fileEng));
                    foreach (var cardType in obj)
                    {
                        if (cardType.Key != "Basic" && cardType.Key != "Expert" && cardType.Key != "Promotion" &&
                            cardType.Key != "Reward") continue;
                        foreach (var card in cardType.Value)
                        {
                            var tmp = JsonConvert.DeserializeObject<Card>(card.ToString());
                            if (languageTag != "enUS")
                            {
                                tmp.LocalizedName = localizedCardNames[tmp.Id];
                            }
                            tempDb.Add(tmp.Id, tmp);
                        }
                    }
                }
                _cardDb = new Dictionary<string, Card>(tempDb);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error loading db: \n" + e);
            }
        }

        public static Card GetCardFromId(string cardId)
        {
            if (cardId == "") return new Card();
            if (_cardDb.ContainsKey(cardId))
            {
                return (Card)_cardDb[cardId].Clone();
            }
            Debug.WriteLine("Could not find entry in db for cardId: " + cardId);
            return new Card(cardId, null, "UNKNOWN", "Minion", "UNKNOWN", 0, "UNKNOWN", 0, 1);
        }
        public Card GetCardFromName(string name)
        {
            if (GetActualCards().Any(c => c.Name.Equals(name)))
            {
                return GetActualCards().FirstOrDefault(c => c.Name.ToLower() == name.ToLower());
            }

            //not sure with all the values here
            Debug.WriteLine("Could not get card from name: " + name);
            return new Card("UNKNOWN", null, "UNKNOWN", "Minion", name, 0, name, 0, 1);
        }

        public List<Card> GetActualCards()
        {
            return (from card in _cardDb.Values
                    where card.Type == "Minion" || card.Type == "Spell" || card.Type == "Weapon"
                    where Helper.IsNumeric(card.Id.ElementAt(card.Id.Length - 1))
                    where Helper.IsNumeric(card.Id.ElementAt(card.Id.Length - 2))
                    where !_invalidCardIds.Any(id => card.Id.Contains(id))
                    select card).ToList();
        }

        public void SetPremadeDeck(Deck deck)
        {
            PlayerDeck.Clear();
            foreach (var card in deck.Cards)
            {
               PlayerDeck.Add(card);
            }
            IsUsingPremade = true;
        }

        public bool PlayerDraw(string cardId)
        {
            PlayerHandCount++;

            if (cardId == "GAME_005")
            {
                OpponentHasCoin = false;
                return true;
            }

            var card = GetCardFromId(cardId);

            if (PlayerDrawn.Contains(card))
            {
                PlayerDrawn.Remove(card);
                card.Count++;
            }
            PlayerDrawn.Add(card);

            if (PlayerDeck.Contains(card))
            {
                var deckCard = PlayerDeck.First(c => c.Equals(card));
                PlayerDeck.Remove(deckCard);
                deckCard.Count--;
                deckCard.InHandCount++;
                PlayerDeck.Add(deckCard);
            }
            else
            {
                return false;
            }
            return true;
        }

        //cards from board(?), thoughtsteal etc
        public void PlayerGet(string cardId)
        {
            PlayerHandCount++;
            if (PlayerDeck.Any(c => c.Id == cardId))
            {
                var card = PlayerDeck.First(c => c.Id == cardId);
                PlayerDeck.Remove(card);
                card.InHandCount++;
                PlayerDeck.Add(card);
            }
        }

        public void PlayerPlayed(string cardId)
        {
            PlayerHandCount--;
            if (PlayerDeck.Any(c => c.Id == cardId))
            {
                var card = PlayerDeck.First(c => c.Id == cardId);
                PlayerDeck.Remove(card);
                card.InHandCount--;
                PlayerDeck.Add(card);
            } 
        }

        public void EnemyDraw()
        {
            EnemyHandCount++;
            OpponentDeckCount--;
        }

        public void EnemyPlayed(string cardId)
        {
           EnemyHandCount--;

            if (cardId == "")
            {
                return;
            }
            if (cardId == "GAME_005")
            {
                OpponentHasCoin = false;
            }
            Card card = GetCardFromId(cardId);
            if (EnemyCards.Any(x => x.Equals(card)))
            {
                EnemyCards.Remove(card);
                card.Count++;
            }
            EnemyCards.Add(card);
        }
        
        public void Mulligan(string cardId)
        {
            PlayerHandCount--;

            Card card = GetCardFromId(cardId);

            if (PlayerDrawn.Any(c => c.Equals(card)))
            {
                var drawnCard = PlayerDrawn.First(c => c.Equals(card));
                PlayerDrawn.Remove(drawnCard);
                if (drawnCard.Count > 1)
                {
                    drawnCard.Count--;
                    PlayerDrawn.Add(drawnCard);
                }
            }
            if (PlayerDeck.Any(c =>  c.Equals(card)))
            {
                var deckCard = PlayerDeck.First(c => c.Equals(card));
                PlayerDeck.Remove(deckCard);
                deckCard.Count++;
                deckCard.InHandCount--;
                PlayerDeck.Add(deckCard);
            }
        }

        public void EnemyMulligan()
        {
            EnemyHandCount--;
            OpponentDeckCount++;
        }

        public void PlayerHandDiscard(string cardId)
        {
            PlayerHandCount--;
            if (PlayerDeck.Any(c => c.Id == cardId))
            {
                var card = PlayerDeck.First(c => c.Id == cardId);
                PlayerDeck.Remove(card);
                card.InHandCount--;
                PlayerDeck.Add(card);
            }
        }

        public bool PlayerDeckDiscard(string cardId)
        {
            Card card = GetCardFromId(cardId);

            if (PlayerDrawn.Contains(card))
            {
                PlayerDrawn.Remove(card);
                card.Count++;
            }
            PlayerDrawn.Add(card);
            
            if (PlayerDeck.Contains(card))
            {
                var deckCard = PlayerDeck.First(c => c.Equals(card));
                PlayerDeck.Remove(deckCard);
                deckCard.Count--;
                PlayerDeck.Add(deckCard);
            }
            else
            {
                return false;
            }
            return true;
        }

        public void OpponentBackToHand(string cardId)
        {
            EnemyHandCount++;
            if (EnemyCards.Any(c => c.Id == cardId))
            {
                var card = EnemyCards.First(c => c.Id == cardId);
                EnemyCards.Remove(card);
                card.Count--;
                if (card.Count > 0)
                {
                    EnemyCards.Add(card);
                }
            }
        }
        public void EnemyHandDiscard()
        {
            EnemyHandCount--;
        }

        public void EnemyDeckDiscard(string cardId)
        {
            OpponentDeckCount--;
            if (string.IsNullOrEmpty(cardId))
            {
                return;
            }
            var card = GetCardFromId(cardId);
            if (EnemyCards.Contains(card))
            {
                EnemyCards.Remove(card);
                card.Count++;
            }
            EnemyCards.Add(card);
        }

        public void EnemySecretTriggered(string cardId)
        {
            if (cardId == "")
            {
                return;
            }
            Card card = GetCardFromId(cardId);
            if (EnemyCards.Contains(card))
            {
                EnemyCards.Remove(card);
                card.Count++;
            }
            EnemyCards.Add(card);
        }


        internal void OpponentGet(string cardId)
        {
            EnemyHandCount++;
        }

        internal void Reset()
        {
            PlayerDrawn.Clear();
            PlayerHandCount = 0;
            EnemyCards.Clear();
            EnemyHandCount = 0;
            OpponentDeckCount = 30;
            OpponentHasCoin = true;
            OpponentHand = new int[10];
            //handPosIndex = 0;
            for (int i = 0; i < 10; i++)
            {
                OpponentHand[i] = -1;
            }
           
        }

        //private int handPosIndex;
        public void OpponentCardPosChange(CardPosChangeArgs args)
        {
            if (args.Action == OpponentHandMovement.Play)
            {
                Debug.WriteLine(string.Format("From {0} to Play", args.From), "CardPosChange");
                OpponentHand[args.From - 1] = -1;
                for (int i = args.From - 1; i < 9; i++)
                {
                    OpponentHand[i] = OpponentHand[i + 1];
                }
                OpponentHand[9] = -1;
            }
            else if (args.Action == OpponentHandMovement.Draw)
            {
                if (EnemyHandCount - 1 < 0 || EnemyHandCount - 1 > 9)
                {
                    //should only be the case if the game crashes
                    return;
                }
                if (OpponentHand[EnemyHandCount - 1] == -1)
                {
                    OpponentHand[EnemyHandCount - 1] = args.Turn;
                    Debug.WriteLine("set " + (EnemyHandCount - 1));
                }
            }
        }

        public List<int> GetOpponentHandAge()
        {
            return OpponentHand.Where(x => x != -1).ToList();
        }
    }
}