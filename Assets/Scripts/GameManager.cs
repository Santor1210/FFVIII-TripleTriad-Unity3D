using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;
using TMPro;
using System.Xml;

[Serializable]
public class TripleTriadCard
{
    public int id;
    public int group;
    public int group_index;
    public string name;
    public List<int> attributes;
    public string element;
    public int level;
}

[Serializable]
public class TripleTriadCardList
{
    public List<TripleTriadCard> cards;
}

public class GameManager : MonoBehaviour
{
    public AIPlayer aiPlayer;

    public List<Card> player1Cards;
    public List<Card> player2Cards;
    public List<Texture2D> textures;
    public List<Transform> boardPositions;
    public SoundManager soundManager;
    public GameObject marker;
    public Transform markerInitialPos;
    public Transform player1marker;
    public Transform player2marker;
    public int currentPlayer;
    public bool vsAI;

    public Color player1Color;
    public Color player2Color;

    public TextMeshPro player1ScoreText;
    public TextMeshPro player2ScoreText;

    public GameObject buttons;
    public GameObject cards;
    public GameObject scores;

    private List<TripleTriadCard> cardsData = new List<TripleTriadCard>();
    private List<Card?> board;
    private Card selectedCard;
    private int scorePlayer1;
    private int scorePlayer2;
    private Dictionary<string, int> directions;
    private List<Vector3> player1CardsPos;
    private List<Vector3> player2CardsPos;
    private List<Quaternion> player1CardsRot;
    private List<Quaternion> player2CardsRot;

    private static GameManager Instance;

    // Singleton pattern to get the instance of GameManager
    public static GameManager GetInstance()
    {
        return Instance;
    }

    public void Awake()
    {
        Instance = this;
    }

    // Initialize the game manager
    private void Start()
    {
        player1CardsPos = new List<Vector3>();
        player2CardsPos = new List<Vector3>();
        player1CardsRot = new List<Quaternion>();
        player2CardsRot = new List<Quaternion>();

        // Store initial positions and rotations of player 1's cards
        foreach (Card card in player1Cards)
        {
            player1CardsPos.Add(card.transform.position);
            player1CardsRot.Add(card.transform.rotation);
        }

        // Store initial positions and rotations of player 2's cards
        foreach (Card card in player2Cards)
        {
            player2CardsPos.Add(card.transform.position);
            player2CardsRot.Add(card.transform.rotation);
        }
    }

    // Start the game with or without AI
    public void StartGame(bool ai)
    {
        vsAI = ai;

        buttons.SetActive(false);
        cards.SetActive(true);
        scores.SetActive(true);

        board = new List<Card?>(new Card?[9]);
        scorePlayer1 = 5;
        scorePlayer2 = 5;
        selectedCard = null;
        player1ScoreText.text = "5";
        player2ScoreText.text = "5";
        ResetCards();
        marker.transform.position = markerInitialPos.position;
        marker.transform.rotation = markerInitialPos.rotation;
        marker.SetActive(true);

        // Define directions for card adjacency checks
        directions = new Dictionary<string, int>
        {
            ["left"] = -1,
            ["right"] = 1,
            ["up"] = -3,
            ["down"] = 3
        };

        LoadCardsData();
        SelectBalancedCards();
        currentPlayer = Random.Range(1, 3);
        StartCoroutine(SpinMarker());

        // If playing against AI and it's AI's turn, make a prediction request
        if (vsAI && currentPlayer == 1)
            if (aiPlayer.alphaZero)
                StartCoroutine(aiPlayer.RequestAlphaZeroPrediction(GetAlphaZeroObservation()));
            else
                StartCoroutine(aiPlayer.RequestPrediction(GetObservation(), GenerateMask(1)));
    }

    // Position the marker on the current player's side
    void PositionMarker()
    {
        if (currentPlayer == 1)
        {
            marker.transform.position = player1marker.position;
            marker.transform.rotation = player1marker.rotation;
        }
        else
        {
            marker.transform.position = player2marker.position;
            marker.transform.rotation = player2marker.rotation;
        }
    }

    // Spin the marker to decide which player starts
    IEnumerator SpinMarker()
    {
        soundManager.soundStart.Play();
        float speed = 50000.0f;
        Vector3 initialRot = marker.transform.eulerAngles;
        float timeSpent = 0;

        // Spin the marker for 1.35 seconds
        while (timeSpent < 1.35f)
        {
            marker.transform.eulerAngles = new Vector3(marker.transform.eulerAngles.x,
                marker.transform.eulerAngles.y + speed * Time.deltaTime, marker.transform.eulerAngles.z);
            timeSpent += Time.deltaTime;
            yield return null;
        }

        marker.transform.eulerAngles = initialRot;
        PositionMarker();
    }

    // Load card data from JSON file
    void LoadCardsData()
    {
        TextAsset cardsJson = Resources.Load<TextAsset>("updated-triple-triad-cards-data");
        if (cardsJson != null)
        {
            // Wrap the JSON in a temporary root object for deserialization
            string jsonToParse = "{\"cards\":" + cardsJson.text + "}";
            TripleTriadCardList cardList = JsonUtility.FromJson<TripleTriadCardList>(jsonToParse);
            cardsData = cardList.cards;
        }
        else
        {
            Debug.LogError("Cannot load the cards data!");
        }
    }

    // Select balanced cards for both players
    void SelectBalancedCards()
    {
        List<TripleTriadCard> cardsDataCopy = new List<TripleTriadCard>(cardsData);
        cardsDataCopy = cardsDataCopy.OrderBy(x => Random.value).ToList(); // Shuffle the cards

        int levelSum1 = 0;

        // Select 5 cards for player 1 and calculate their total level
        for (int i = 0; i < 5; i++)
        {
            int index = Random.Range(0, cardsDataCopy.Count);
            TripleTriadCard card = cardsDataCopy[index];
            AddCard(card, this.player1Cards[i]);
            levelSum1 += card.level;
            cardsDataCopy.RemoveAt(index);
        }

        List<TripleTriadCard> levelAppropriateCards = new List<TripleTriadCard>();

        // Handle edge case where levelSum1 is high or low
        if (levelSum1 >= 49)
        {
            levelAppropriateCards = cardsDataCopy.Where(card => card.level == 10).ToList();
            if (levelSum1 == 50)
            {
                for (int i = 0; i < 5; i++)
                {
                    int index = Random.Range(0, levelAppropriateCards.Count);
                    TripleTriadCard card = cardsDataCopy[index];
                    AddCard(card, this.player2Cards[i]);
                    cardsDataCopy.RemoveAt(index);
                }
            }
            else
            {
                int index = Random.Range(0, levelAppropriateCards.Count);
                TripleTriadCard card = cardsDataCopy[index];
                AddCard(card, this.player2Cards[0]);
                cardsDataCopy.RemoveAt(index);
                levelAppropriateCards = cardsDataCopy.Where(card => card.level == 9).ToList();
                for (int i = 0; i < 4; i++)
                {
                    int nineIndex = Random.Range(0, levelAppropriateCards.Count);
                    TripleTriadCard nineCard = cardsDataCopy[nineIndex];
                    AddCard(nineCard, this.player2Cards[i]);
                    cardsDataCopy.RemoveAt(nineIndex);
                }
            }
        }
        else if (levelSum1 <= 6)
        {
            levelAppropriateCards = cardsDataCopy.Where(card => card.level == 1).ToList();
            if (levelSum1 == 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    int index = Random.Range(0, levelAppropriateCards.Count);
                    TripleTriadCard card = cardsDataCopy[index];
                    AddCard(card, this.player2Cards[i]);
                    cardsDataCopy.RemoveAt(index);
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    int oneIndex = Random.Range(0, levelAppropriateCards.Count);
                    TripleTriadCard oneCard = cardsDataCopy[oneIndex];
                    AddCard(oneCard, this.player2Cards[i]);
                    cardsDataCopy.RemoveAt(oneIndex);
                }

                levelAppropriateCards = cardsDataCopy.Where(card => card.level == 2).ToList();
                int index = Random.Range(0, levelAppropriateCards.Count);
                TripleTriadCard card = cardsDataCopy[index];
                AddCard(card, this.player2Cards[4]);
                cardsDataCopy.RemoveAt(index);
            }
        }
        else
        {
            int levelSum2 = 0;
            int cardNumber = 0;
            while (levelSum2 < levelSum1 && cardNumber < 4)
            {
                int index = Random.Range(0, cardsDataCopy.Count);
                TripleTriadCard card = cardsDataCopy[index];

                int newLevelSum2 = levelSum2 + card.level;
                if (newLevelSum2 < levelSum1 && (4 - cardNumber) * 10 > levelSum1 - newLevelSum2 &&
                    levelSum1 - newLevelSum2 >= this.player1Cards.Count - 1 - cardNumber)
                {
                    AddCard(card, this.player2Cards[cardNumber]);
                    cardsDataCopy.RemoveAt(index);
                    levelSum2 = newLevelSum2;
                    cardNumber++;
                }
            }
            levelAppropriateCards = cardsDataCopy.Where(card => card.level == (levelSum1 - levelSum2)).ToList();
            int lastCardIndex = Random.Range(0, levelAppropriateCards.Count);
            TripleTriadCard lastCard = cardsDataCopy[lastCardIndex];
            AddCard(lastCard, this.player2Cards[4]);
            cardsDataCopy.RemoveAt(lastCardIndex);
        }
    }

    // Add card attributes and textures to the card object
    private void AddCard(TripleTriadCard cardData, Card card)
    {
        card.Initialize(cardData.attributes[0], cardData.attributes[1], cardData.attributes[2], cardData.attributes[3]);
        Vector4 offset = new Vector2(cardData.group_index % 2 / 2.0f, 0.75f - (cardData.group_index / 2) * 0.25f);
        Material cardMaterial = card.GetComponent<MeshRenderer>().sharedMaterial;
        cardMaterial.SetTexture("_MainTex", textures[cardData.group]);
        cardMaterial.SetVector("_Offset", offset);
        card.GetComponent<MeshRenderer>().sharedMaterial = cardMaterial;
    }

    // Check and convert adjacent cards if conditions are met
    void CheckAndConvertAdjacentCards(int index)
    {
        int adjDirection = index + directions["left"];
        if (adjDirection >= 0 && adjDirection % 3 != 2 && board[adjDirection] != null)
        {
            Card adjCard = board[adjDirection];
            if (adjCard.player != selectedCard.player && adjCard.right < selectedCard.left)
            {
                StartCoroutine(FlipCard(adjCard));
                adjCard.player = adjCard.player == 1 ? 2 : 1;
                scorePlayer1 += adjCard.player == 1 ? 1 : -1;
                scorePlayer2 += adjCard.player == 2 ? 1 : -1;
                player1ScoreText.text = scorePlayer1.ToString();
                player2ScoreText.text = scorePlayer2.ToString();
            }
        }

        adjDirection = index + directions["right"];
        if (adjDirection < 9 && adjDirection % 3 != 0 && board[adjDirection] != null)
        {
            Card adjCard = board[adjDirection];
            if (adjCard.player != selectedCard.player && adjCard.left < selectedCard.right)
            {
                StartCoroutine(FlipCard(adjCard));
                adjCard.player = adjCard.player == 1 ? 2 : 1;
                scorePlayer1 += adjCard.player == 1 ? 1 : -1;
                scorePlayer2 += adjCard.player == 2 ? 1 : -1;
                player1ScoreText.text = scorePlayer1.ToString();
                player2ScoreText.text = scorePlayer2.ToString();
            }
        }

        adjDirection = index + directions["up"];
        if (index + directions["up"] >= 0 && board[adjDirection] != null)
        {
            Card adjCard = board[adjDirection];
            if (adjCard.player != selectedCard.player && adjCard.bot < selectedCard.top)
            {
                StartCoroutine(FlipCard(adjCard));
                adjCard.player = adjCard.player == 1 ? 2 : 1;
                scorePlayer1 += adjCard.player == 1 ? 1 : -1;
                scorePlayer2 += adjCard.player == 2 ? 1 : -1;
                player1ScoreText.text = scorePlayer1.ToString();
                player2ScoreText.text = scorePlayer2.ToString();
            }
        }

        adjDirection = index + directions["down"];
        if (index + directions["down"] < 9 && board[adjDirection] != null)
        {
            Card adjCard = board[adjDirection];
            if (adjCard.player != selectedCard.player && adjCard.top < selectedCard.bot)
            {
                StartCoroutine(FlipCard(adjCard));
                adjCard.player = adjCard.player == 1 ? 2 : 1;
                scorePlayer1 += adjCard.player == 1 ? 1 : -1;
                scorePlayer2 += adjCard.player == 2 ? 1 : -1;
                player1ScoreText.text = scorePlayer1.ToString();
                player2ScoreText.text = scorePlayer2.ToString();
            }
        }
        selectedCard.played = true;
        selectedCard = null;
        if (CheckGameEnd())
        {
            EndGame();
        }
    }

    // Flip the card animation
    IEnumerator FlipCard(Card card)
    {
        float duration = 0.1f;
        float time = 0;
        Vector3 startPoint = card.transform.localPosition;
        Vector3 endPoint = new Vector3(card.transform.localPosition.x, startPoint.y + 0.8f,
            card.transform.localPosition.z);
        soundManager.soundCapture.Play();
        while (time < duration)
        {
            card.transform.localPosition = Vector3.Lerp(startPoint, endPoint, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        card.transform.localPosition = endPoint;

        // Calculate the target rotation as 180 degrees around the Y axis
        Quaternion startRotation = card.transform.rotation;
        Quaternion endRotation = Quaternion.Euler(card.transform.rotation.eulerAngles.x, card.transform.rotation.eulerAngles.y, 180);

        time = 0;
        duration = 0.2f;

        while (time < duration)
        {
            // Interpolate the rotation from the start rotation to the end rotation over time
            card.transform.rotation = Quaternion.Lerp(startRotation, endRotation, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        // Ensure the card ends up exactly at the final rotation
        card.transform.rotation = endRotation;

        time = 0;
        duration = 0.1f;
        while (time < duration)
        {
            card.transform.localPosition = Vector3.Lerp(endPoint, startPoint, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        card.transform.localPosition = startPoint;
    }

    // Get the current game observation as an array of floats
    float[] GetObservation()
    {
        List<float> observation = new List<float>();

        // Add the player's turn
        observation.AddRange(currentPlayer == 1 ? new float[] { 1, 0 } : new float[] { 0, 1 });

        // Add each player's score
        observation.Add(scorePlayer1 / 10.0f);
        observation.Add(scorePlayer2 / 10.0f);

        // Add information about player 1's cards
        foreach (Card card in player1Cards)
        {
            if (card.played)
            {
                observation.AddRange(new float[6]); // Add six zeros if the card has been played
            }
            else
            {
                observation.Add(card.top / 10.0f);
                observation.Add(card.right / 10.0f);
                observation.Add(card.bot / 10.0f);
                observation.Add(card.left / 10.0f);
                observation.AddRange(new float[] { 1, 0 }); // Indicator of player 1's unplayed card
            }
        }

        // Add board information
        foreach (Card? slot in board)
        {
            if (slot == null)
            {
                observation.AddRange(new float[6]); // Empty board slot
            }
            else
            {
                observation.Add(slot.top / 10.0f);
                observation.Add(slot.right / 10.0f);
                observation.Add(slot.bot / 10.0f);
                observation.Add(slot.left / 10.0f);
                observation.AddRange(slot.player == 1 ? new float[] { 1, 0 } : new float[] { 0, 1 });
            }
        }

        // Repeat a similar process for player 2's cards
        foreach (Card card in player2Cards)
        {
            if (card.played)
            {
                observation.AddRange(new float[6]);
            }
            else
            {
                observation.Add(card.top / 10.0f);
                observation.Add(card.right / 10.0f);
                observation.Add(card.bot / 10.0f);
                observation.Add(card.left / 10.0f);
                observation.AddRange(new float[] { 0, 1 }); // Indicator of player 2's unplayed card
            }
        }

        return observation.ToArray();
    }

    // Get the AlphaZero observation as a dictionary of strings
    public Dictionary<string, string> GetAlphaZeroObservation()
    {
        // 9x9 Board - each sub 3x3 grid represents a card
        float[,,] board = new float[9, 9, 1]; // Extra dimension for simplicity in JSON
        int[] board_cards = new int[9]; // Example, needs your actual implementation to fill this

        // Fill the board matrix
        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                board[i, j, 0] = 0; // Default value
            }
        }

        // Set card values in the board
        for (int idx = 0; idx < this.board.Count; idx++)
        {
            Card card = this.board[idx];
            if (card != null)
            {
                int baseRow = (idx / 3) * 3;
                int baseCol = (idx % 3) * 3;
                board[baseRow + 0, baseCol + 1, 0] = card.top / 10.0f;
                board[baseRow + 1, baseCol + 2, 0] = card.right / 10.0f;
                board[baseRow + 2, baseCol + 1, 0] = card.bot / 10.0f;
                board[baseRow + 1, baseCol + 0, 0] = card.left / 10.0f;
                board[baseRow + 1, baseCol + 1, 0] = card.player == 1 ? 1 : -1; // Player indicator

                // Determine if the position is occupied by player 1
                board_cards[idx] = card.player == 1 ? 1 : 0;
            }
        }

        // Player 1 and Player 2 cards
        float[,,] player1Cards = new float[15, 3, 1];
        float[,,] player2Cards = new float[15, 3, 1];
        for (int cardIndex = 0; cardIndex < 5; cardIndex++) // Assuming 5 cards per player
        {
            player1Cards = FillCardMatrix(this.player1Cards, 1); // Fill for player 1
            player2Cards = FillCardMatrix(this.player2Cards, 2); // Fill for player 2
        }

        // Convert matrix and arrays to JSON serializable strings
        Dictionary<string, string> result = new Dictionary<string, string>();
        result["board"] = MatrixToString(board);
        result["player_1_cards"] = MatrixToString(player1Cards);
        result["player_2_cards"] = MatrixToString(player2Cards);
        result["board_cards"] = string.Join(",", board_cards); // Convert the bitarray to JSON string

        return result;
    }

    // Fill the card matrix for the given player
    float[,,] FillCardMatrix(List<Card> playerCards, int playerNumber)
    {
        float[,,] matrix = new float[15, 3, 1];
        for (int cardIndex = 0; cardIndex < 5; cardIndex++)
        {
            Card card = playerCards[cardIndex];
            int baseRow = cardIndex * 3; // Each card has a 3x3 submatrix in the 15x3 matrix
            if (card.played)
            {
                // Fill the entire 3x3 submatrix with zeros if the card is played
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        matrix[baseRow + i, j, 0] = 0;
            }
            else
            {
                // Fill with card values if not played
                matrix[baseRow + 0, 1, 0] = card.top / 10.0f;
                matrix[baseRow + 1, 2, 0] = card.right / 10.0f;
                matrix[baseRow + 2, 1, 0] = card.bot / 10.0f;
                matrix[baseRow + 1, 0, 0] = card.left / 10.0f;
                matrix[baseRow + 1, 1, 0] = (playerNumber == 1) ? 1 : -1; // Player indicator
            }
        }
        return matrix;
    }

    // Convert the matrix to a string for serialization
    string MatrixToString(float[,,] matrix)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                sb.Append(matrix[i, j, 0] + " ");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    // Make the AI play the given card at the given position
    public void AIPlay(int card, int position)
    {
        if (card == 5)
            return;
        StartCoroutine(AIPlayRoutine(card, position));
    }

    // Coroutine for the AI to play a card
    private IEnumerator AIPlayRoutine(int card, int position)
    {
        yield return new WaitForSeconds(1f);
        SelectCard(player1Cards[card]);

        // Wait for a second
        yield return new WaitForSeconds(1.5f);

        PlaceCard(position, boardPositions[position].position);
    }

    // Check if the game has ended
    bool CheckGameEnd()
    {
        return board.All(slot => slot != null);
    }

    // Select a card to be played
    public void SelectCard(Card card)
    {
        soundManager.cursorMove.Play();
        UnselectCard();
        int direction = this.currentPlayer == 1 ? -1 : 1;
        card.transform.Translate(0.7f * direction, 0.0f, 0.0f);
        selectedCard = card;
    }

    // Unselect the current selected card
    private void UnselectCard()
    {
        if (this.selectedCard == null)
            return;
        int direction = this.currentPlayer == 1 ? 1 : -1;
        selectedCard.transform.Translate(0.7f * direction, 0.0f, 0.0f);
        selectedCard = null;
    }

    // Place the selected card on the board
    public void PlaceCard(int index, Vector3 position)
    {
        if (this.selectedCard == null || board[index] != null)
            return;
        board[index] = this.selectedCard;
        StartCoroutine(AnimateCard(position, index));
    }

    // Generate the back of the card
    private void GenerateCardBack()
    {
        GameObject backCard = Instantiate(selectedCard.gameObject, selectedCard.transform);
        backCard.transform.localPosition = Vector3.zero;
        backCard.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 180.0f);
        backCard.transform.localScale = Vector3.one;
        selectedCard.back = backCard;
        Material clonedMaterial = Instantiate(selectedCard.GetComponent<Renderer>().material);
        clonedMaterial.SetColor("_BackgroundColor", selectedCard.player == 2 ? this.player1Color : this.player2Color);
        backCard.GetComponent<MeshRenderer>().sharedMaterial = clonedMaterial;
    }

    // Animate the card placement on the board
    IEnumerator AnimateCard(Vector3 finalPoint, int index)
    {
        float time = 0;
        float duration = 0.3f;
        Vector3 startPoint = selectedCard.transform.position;
        Vector3 midPoint = new Vector3(selectedCard.transform.position.x, 15.0f, selectedCard.transform.position.z);
        soundManager.soundCard.Play();
        while (time < duration)
        {
            selectedCard.transform.position = Vector3.Lerp(startPoint, midPoint, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        selectedCard.transform.position = midPoint;

        soundManager.soundCard.Play();
        time = 0;
        while (time < duration)
        {
            selectedCard.transform.position = Vector3.Lerp(midPoint, finalPoint, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        selectedCard.transform.position = finalPoint;
        GenerateCardBack();
        CheckAndConvertAdjacentCards(index);
        this.currentPlayer = this.currentPlayer == 1 ? 2 : 1;
        PositionMarker();

        // If playing against AI and it's AI's turn, make a prediction request
        if (vsAI && currentPlayer == 1)
            if (aiPlayer.alphaZero)
                StartCoroutine(aiPlayer.RequestAlphaZeroPrediction(GetAlphaZeroObservation()));
            else
                StartCoroutine(aiPlayer.RequestPrediction(GetObservation(), GenerateMask(1)));
    }

    // End the game and show the buttons
    private void EndGame()
    {
        buttons.SetActive(true);
    }

    // Reset the cards to their initial positions
    private void ResetCards()
    {
        for (int i = 0; i < player1Cards.Count; i++)
        {
            player1Cards[i].transform.position = player1CardsPos[i];
            player1Cards[i].transform.rotation = player1CardsRot[i];
            Destroy(player1Cards[i].back);
            player1Cards[i].player = 1;
            player1Cards[i].played = false;
            player1Cards[i].GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BackgroundColor", this.player1Color);
        }
        for (int i = 0; i < player2Cards.Count; i++)
        {
            player2Cards[i].transform.position = player2CardsPos[i];
            player2Cards[i].transform.rotation = player2CardsRot[i];
            Destroy(player2Cards[i].back);
            player2Cards[i].player = 2;
            player2Cards[i].played = false;
            player2Cards[i].GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BackgroundColor", this.player2Color);
        }
    }

    // Generate mask for valid moves
    public bool[] GenerateMask(int playerNumber)
    {
        // Initialize two separate masks for card and position dimensions
        bool[] cardsMask = new bool[6]; // 5 cards + 1 wait action
        bool[] positionsMask = new bool[10]; // 9 positions + 1 wait action
        Array.Fill(cardsMask, true); // By default, all actions are valid
        Array.Fill(positionsMask, true); // By default, all positions are valid

        if (currentPlayer != playerNumber)
        {
            // If it is NOT the player's turn, all actions are invalid except 'wait'
            Array.Fill(cardsMask, false);
            Array.Fill(positionsMask, false);
            cardsMask[5] = true; // Enable only the 'wait' action for cards
            positionsMask[9] = true; // Enable only the 'wait' action for positions
        }
        else
        {
            // It is the player's turn
            // Invalidate already played cards
            List<Card> playerCards = playerNumber == 1 ? player1Cards : player2Cards;
            for (int cardIndex = 0; cardIndex < playerCards.Count; cardIndex++)
            {
                if (playerCards[cardIndex].played)
                {
                    cardsMask[cardIndex] = false;
                }
            }

            // Invalidate already occupied board positions
            for (int positionIndex = 0; positionIndex < board.Count; positionIndex++)
            {
                if (board[positionIndex] != null)
                {
                    positionsMask[positionIndex] = false;
                }
            }

            // The 'wait' action is always invalid during the player's turn
            cardsMask[5] = false;
            positionsMask[9] = false;
        }

        // Combine the card and position masks into a single structure
        return cardsMask.Concat(positionsMask).ToArray();
    }
}

