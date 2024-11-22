using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ChatDisplay : MonoBehaviour
{
    public Text chatOutputText; // Unity UIのTextコンポーネントをアサインする
    public Image responseImage; // ChatResponseOutputのImageへの参照

    void Start()
    {
        // 1秒ごとにAPIリクエストを送信するコルーチンを開始
        StartCoroutine(SendPeriodicRequest());
    }

    IEnumerator SendPeriodicRequest()
    {
        while (true)
        {
            yield return SendRequest();
            yield return new WaitForSeconds(1f); // 1秒待機
        }
    }
    IEnumerator SendRequest()
    {
        string url = "https://0631q0ohzb.execute-api.ap-northeast-1.amazonaws.com/prod/panorama";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + webRequest.error);
            }
            else
            {
                string responseText = webRequest.downloadHandler.text;
                Debug.Log(responseText);
                
                LambdaResponse lambdaResponse = JsonUtility.FromJson<LambdaResponse>(responseText);
                // JSONの配列部分をオブジェクトとして解析するための修正
                string correctedJson = "{\"messages\":" + lambdaResponse.body + "}";
                ChatMessageList messageList = JsonUtility.FromJson<ChatMessageList>(correctedJson);
                DisplayMessages(messageList.messages);  // 修正: messageList.messages を渡す
            }
        }
    }

    void DisplayMessages(ChatMessage[] messages)
    {
        string fullText = "";
        foreach (var message in messages)
        {
            fullText += message.chatgpt_response;
        }

        chatOutputText.text = fullText; // 結合したテキストを設定

        // テキストが空の場合は画像を非表示に
        responseImage.gameObject.SetActive(!string.IsNullOrEmpty(fullText));
    }


    [System.Serializable]
    public class LambdaResponse
    {
        public int statusCode;
        public string body;
    }
    [System.Serializable]
    public class ChatMessageList
    {
        public ChatMessage[] messages;
    }

    [System.Serializable]
    public class ChatMessage
    {
        public string timestamp;
        public string user_message;
        public string chatgpt_response;
    }


}
