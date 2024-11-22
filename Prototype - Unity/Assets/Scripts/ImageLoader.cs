using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ImageLoader : MonoBehaviour
{
    public Canvas parentCanvas;
    public Vector2 imageSize = new Vector2(500, 500);
    public Vector2 imageSize_all = new Vector2(200, 200); // 画像サイズを150x150に設定
    private List<int> imageIndexes = new List<int>(); // 画像インデックスのリスト
    private List<GameObject> currentImages = new List<GameObject>(); // 現在表示中の画像オブジェクトのリスト
    private Dictionary<string, Vector2> imagePositions = new Dictionary<string, Vector2>(); // 画像の名前とその位置を記憶するディクショナリ
    private bool showingAllImages = false; // 現在すべての画像を表示中かどうか
    private List<int> previousImageIndexes = new List<int>(); // 前回表示された画像インデックスのリスト

    void Start()
    {
        StartCoroutine(PeriodicDownloadAndDisplay());
    }

    IEnumerator PeriodicDownloadAndDisplay()
    {
        while (true)
        {
            yield return DownloadImageListAndDisplay();
            yield return new WaitForSeconds(1f); // 2秒待機
        }
    }

    IEnumerator DownloadImageListAndDisplay()
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
                ProcessImageData(responseText);

                if (AreImageIndexesEqual(imageIndexes, previousImageIndexes))
                {
                    // 画像インデックスが変わらない場合は何もしない
                    yield break;
                }

                if (imageIndexes.Count == 1 && !showingAllImages)
                {
                    Debug.Log("0mai");
                    StartCoroutine(TransitionToAllImages());
                }
                else if (imageIndexes.Count > 1 && showingAllImages)
                {
                    StartCoroutine(TransitionToImages());
                }
                else
                {
                    StartCoroutine(TransitionToImages());
                }

                previousImageIndexes = new List<int>(imageIndexes);
            }
        }
    }

    bool AreImageIndexesEqual(List<int> list1, List<int> list2)
    {
        if (list1.Count != list2.Count)
            return false;

        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i] != list2[i])
                return false;
        }

        return true;
    }

    void ProcessImageData(string json)
    {
        LambdaResponse lambdaResponse = JsonUtility.FromJson<LambdaResponse>(json);
        string correctedJson = "{\"messages\":" + lambdaResponse.body + "}";
        ChatMessageList messageList = JsonUtility.FromJson<ChatMessageList>(correctedJson);

        if (messageList.messages.Count > 0 && messageList.messages[0].list_images != null)
        {
            imageIndexes = messageList.messages[0].list_images;
        }
    }

    void UpdateCurrentImages()
    {
        HashSet<int> imageSet = new HashSet<int>(imageIndexes);

        for (int i = currentImages.Count - 1; i >= 0; i--)
        {
            GameObject imageObj = currentImages[i];
            int imageIndex = int.Parse(imageObj.name.Replace("Image_", ""));
            if (!imageSet.Contains(imageIndex))
            {
                Destroy(imageObj);
                currentImages.RemoveAt(i);
            }
        }
    }

    IEnumerator TransitionToAllImages()
{
    ClearImages();
    List<GameObject> newImages = new List<GameObject>();

    Sprite[] allSprites = Resources.LoadAll<Sprite>("Images");

    RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
    float canvasHeight = canvasRect.rect.height;

    float canvasMinX = -4000f;
    float canvasMaxX = 4000f;
    float segmentWidth = 3000f;
    float leftSegmentStart = -3500f;
    float rightSegmentStart = 1000f;

    int numberOfSegments = 4;

    List<Vector2> availablePositions = new List<Vector2>();

    for (int seg = 0; seg < numberOfSegments; seg++)
    {
        float segmentStartX = (seg < 2) ? leftSegmentStart : rightSegmentStart;
        float segmentOffsetX = (seg % 2) * segmentWidth;
        float xStart = segmentStartX + segmentOffsetX;
        float xEnd = xStart + segmentWidth;

        for (float x = xStart + imageSize_all.x / 2; x < xEnd - imageSize_all.x / 2; x += imageSize_all.x + 20)
        {
            float wrappedX = WrapAround(x, canvasMinX, canvasMaxX);
            if (wrappedX < -700 || wrappedX > 700) // [-700, 700]の範囲を除外
            {
                for (float y = canvasHeight / 2 - imageSize_all.y / 2; y > -canvasHeight / 2 + imageSize_all.y / 2; y -= imageSize_all.y + 20)
                {
                    availablePositions.Add(new Vector2(wrappedX, y));
                }
            }
        }
    }

    // シャッフル処理
    for (int i = 0; i < availablePositions.Count; i++)
    {
        Vector2 temp = availablePositions[i];
        int randomIndex = UnityEngine.Random.Range(i, availablePositions.Count);
        availablePositions[i] = availablePositions[randomIndex];
        availablePositions[randomIndex] = temp;
    }

    for (int i = 0; i < allSprites.Length; i++)
    {
        Sprite imageSprite = allSprites[i];
        if (imageSprite != null)
        {
            GameObject imageObj = new GameObject($"Image_{imageSprite.name}");
            imageObj.transform.SetParent(parentCanvas.transform, false);
            Image image = imageObj.AddComponent<Image>();
            image.sprite = imageSprite;
            RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = imageSize_all * 0.1f; // 初期サイズを小さく設定

            rectTransform.anchoredPosition = availablePositions[i % availablePositions.Count];

            newImages.Add(imageObj);
        }
        else
        {
            Debug.LogError($"Image {imageSprite.name} could not be loaded.");
        }
    }

    float duration = 0.5f; // トランジションの時間を1秒に設定
    float elapsedTime = 0f;

    while (elapsedTime < duration)
    {
        elapsedTime += Time.deltaTime;
        float t = elapsedTime / duration;

        for (int i = 0; i < newImages.Count; i++)
        {
            GameObject newImageObj = newImages[i];
            RectTransform newRectTransform = newImageObj.GetComponent<RectTransform>();
            Vector2 startPos = availablePositions[i % availablePositions.Count];
            Vector2 endPos = imagePositions.ContainsKey(newImageObj.name) ? imagePositions[newImageObj.name] : startPos;
            newRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            newRectTransform.sizeDelta = Vector2.Lerp(imageSize_all * 0.1f, imageSize_all, t); // サイズを小さくから元のサイズに補間
        }

        yield return null;
    }

    ClearImages();

    currentImages.AddRange(newImages);

    showingAllImages = true;
}


    IEnumerator TransitionToFourImages()
    {
        ClearImages();
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        float segmentWidth = 3000f;
        float leftSegmentStart = -3500f;
        float rightSegmentStart = 1000f;

        float[] xPositions = {
            leftSegmentStart + segmentWidth / 3 - imageSize.x / 2,
            leftSegmentStart + 2 * segmentWidth / 3 - imageSize.x / 2,
            rightSegmentStart + segmentWidth / 3 - imageSize.x / 2,
            rightSegmentStart + 2 * segmentWidth / 3 - imageSize.x / 2
        };

        float duration = 1.0f; // トランジションの時間を1秒に設定
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            for (int i = 0; i < imageIndexes.Count; i++)
            {
                string imagePath = $"Images/{imageIndexes[i]}";
                Sprite imageSprite = Resources.Load<Sprite>(imagePath);
                if (imageSprite != null)
                {
                    GameObject imageObj = FindCurrentImage(imageIndexes[i]);
                    if (imageObj == null)
                    {
                        imageObj = new GameObject($"Image_{imageIndexes[i]}");
                        imageObj.transform.SetParent(parentCanvas.transform, false);
                        Image image = imageObj.AddComponent<Image>();
                        image.sprite = imageSprite;
                        currentImages.Add(imageObj);

                    }

                    RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
                    Vector2 startPos = rectTransform.anchoredPosition;
                    Vector2 endPos = new Vector2(xPositions[i], 0);
                    rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                    rectTransform.sizeDelta = Vector2.Lerp(imageSize_all, imageSize, t);
                }
                else
                {
                    Debug.LogError($"Image at {imagePath} could not be loaded.");
                }
            }

            yield return null;
        }

        showingAllImages = false;
    }
    IEnumerator TransitionToImages()
    {
        ClearImages();
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        float canvasHeight = canvasRect.rect.height;

        // 左右の表示領域の境界
        float leftBoundary = -700f;
        float rightBoundary = 700f;

        // 画像の数に応じて位置を計算する
        int imageCount = imageIndexes.Count;
        int halfCount = (imageCount + 1) / 2; // 左側に割り当てる画像の数（奇数の場合は左側に1つ多くする）

        // 左右の領域の幅
        float leftWidth = Mathf.Abs(leftBoundary - (-canvasRect.rect.width / 2));
        float rightWidth = Mathf.Abs((canvasRect.rect.width / 2) - rightBoundary);

        // ステップ幅を計算
        float leftStep = leftWidth / (halfCount + 1);
        float rightStep = rightWidth / (imageCount - halfCount + 1);

        List<Vector2> startPositions = new List<Vector2>();
        List<Vector2> endPositions = new List<Vector2>();

        // 左側の位置を計算
        for (int i = 0; i < halfCount; i++)
        {
            float posX = leftBoundary - leftStep * (halfCount - i);
            startPositions.Add(new Vector2(posX, 0)); // 中心から開始
            endPositions.Add(new Vector2(posX, 0)); // 画面内で終了
        }

        // 右側の位置を計算
        for (int i = 0; i < imageCount - halfCount; i++)
        {
            float posX = rightBoundary + rightStep * (i + 1);
            startPositions.Add(new Vector2(posX, 0)); // 中心から開始
            endPositions.Add(new Vector2(posX, 0)); // 画面内で終了
        }

        float duration = 0.7f; // トランジションの時間を1秒に設定
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            for (int i = 0; i < imageIndexes.Count; i++)
            {
                string imagePath = $"Images/{imageIndexes[i]}";
                Sprite imageSprite = Resources.Load<Sprite>(imagePath);
                if (imageSprite != null)
                {
                    GameObject imageObj = FindCurrentImage(imageIndexes[i]);
                    if (imageObj == null)
                    {
                        imageObj = new GameObject($"Image_{imageIndexes[i]}");
                        imageObj.transform.SetParent(parentCanvas.transform, false);
                        Image image = imageObj.AddComponent<Image>();
                        image.sprite = imageSprite;
                        currentImages.Add(imageObj);
                    }

                    RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
                    Vector2 startPos = startPositions[i];
                    Vector2 endPos = endPositions[i];
                    rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                    rectTransform.sizeDelta = Vector2.Lerp(imageSize_all, imageSize, t);
                }
                else
                {
                    Debug.LogError($"Image at {imagePath} could not be loaded.");
                }
            }

            yield return null;
        }

        showingAllImages = false;
    }

    GameObject FindCurrentImage(int imageIndex)
    {
        foreach (GameObject imageObj in currentImages)
        {
            if (imageObj.name == $"Image_{imageIndex}")
            {
                return imageObj;
            }
        }
        return null;
    }

    void ClearImages()
    {
        foreach (GameObject imageObj in currentImages)
        {
            Destroy(imageObj);
        }
        currentImages.Clear();
    }

    float WrapAround(float value, float min, float max)
    {
        if (value > max)
        {
            return min + (value - max);
        }
        else if (value < min)
        {
            return max - (min - value);
        }
        return value;
    }
}


[Serializable]
public class LambdaResponse
{
    public int statusCode;
    public string body;
}

[Serializable]
public class ChatMessageList
{
    public List<ChatMessage> messages;
}

[Serializable]
public class ChatMessage
{
    public string timestamp;
    public string user_message;
    public string chatgpt_response;
    public List<int> list_images;
}
