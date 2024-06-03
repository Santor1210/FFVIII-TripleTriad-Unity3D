using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Collections.Generic;

[System.Serializable] // Ensure to mark the class as Serializable.
public class PredictionRequest
{
    public float[] observations;
    public bool[] masks;

    public PredictionRequest(float[] observations, bool[] masks)
    {
        this.observations = observations;
        this.masks = masks;
    }
}

[System.Serializable]
public class AlphaZeroObservation
{
    public string board;
    public string player_1_cards;
    public string player_2_cards;
    public string board_cards;

    public AlphaZeroObservation(Dictionary<string, string> observations)
    {
        this.board = observations["board"];
        this.player_1_cards = observations["player_1_cards"];
        this.player_2_cards = observations["player_2_cards"];
        this.board_cards = observations["board_cards"];
    }
}

public class AIPlayer : MonoBehaviour
{
    private readonly string baseURL = "http://localhost:5000/predict";
    public bool alphaZero;

    // Method to send observations and masks to the server and receive actions
    public IEnumerator RequestPrediction(float[] observations, bool[] masks)
    {
        // Prepare the data to send by creating an instance of PredictionRequest
        PredictionRequest requestData = new PredictionRequest(observations, masks);

        // Serialize the PredictionRequest instance to JSON
        string data = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(data);
        UnityWebRequest request = new UnityWebRequest(baseURL, "POST");
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request to the server
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
        }
        else
        {
            string responseText = request.downloadHandler.text;
            Debug.Log(responseText);
            // Remove unnecessary characters such as brackets, whitespace, newlines, and the closing bracket character
            responseText = responseText.Trim(new char[] { '[', ']', ' ', '\n', '\r' });

            // Split the numbers using a comma as a separator
            string[] numbers = responseText.Split(',');

            // Ensure there are enough elements in the array before trying to access them
            if (numbers.Length >= 2)
            {
                // Convert the string array elements to integers
                int cardIndex = int.Parse(numbers[0].Trim());
                int positionIndex = int.Parse(numbers[1].Trim());
                GameManager.GetInstance().AIPlay(cardIndex, positionIndex);
            }
            else
            {
                Debug.LogError("The response format is not as expected: " + responseText);
            }

        }
    }

    public IEnumerator RequestAlphaZeroPrediction(Dictionary<string, string> observations)
    {
        AlphaZeroObservation requestData = new AlphaZeroObservation(observations);
        // Serialize the AlphaZeroObservation instance to JSON
        string data = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(data);
        UnityWebRequest request = new UnityWebRequest(baseURL, "POST");
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request to the server
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
        }
        else
        {
            string responseText = request.downloadHandler.text;
            int action = int.Parse(responseText);
            int cardIndex = action / 9;
            int positionIndex = action % 9;
            GameManager.GetInstance().AIPlay(cardIndex, positionIndex);
        }
    }

}
