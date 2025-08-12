// File: UI/UILineDrawer.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UILineDrawer : MonoBehaviour
{
    public Sprite lineSprite;
    public Color lineColor = Color.red;
    public float lineWidth = 10f; // Tăng độ dày một chút

    private readonly List<GameObject> segments = new List<GameObject>();

    public void DrawLine(List<Vector2> localPoints, float duration, System.Action onComplete = null)
    {
        ClearLine();

        if (lineSprite == null)
        {
            Debug.LogError("[UILineDrawer] Chưa gán lineSprite!");
            onComplete?.Invoke(); // Vẫn gọi onComplete để game không bị treo
            return;
        }

        if (localPoints == null || localPoints.Count < 2)
        {
            onComplete?.Invoke();
            return;
        }

        // Tạo các đoạn thẳng giữa các điểm local
        for (int i = 0; i < localPoints.Count - 1; i++)
        {
            CreateSegment(localPoints[i], localPoints[i + 1]);
        }

        StartCoroutine(HideAfterDelay(duration, onComplete));
    }

    private void CreateSegment(Vector2 start, Vector2 end)
    {
        GameObject go = new GameObject("LineSegment", typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();

        Image img = go.GetComponent<Image>();
        img.sprite = lineSprite;
        img.color = lineColor;
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Vector2 dir = end - start;
        float length = dir.magnitude;

        rt.sizeDelta = new Vector2(length, lineWidth);
        rt.anchoredPosition = (start + end) / 2f; // 
        rt.rotation = Quaternion.FromToRotation(Vector3.right, dir);

        segments.Add(go);
    }


    private System.Collections.IEnumerator HideAfterDelay(float delay, System.Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        ClearLine();
        onComplete?.Invoke();
    }

    public void ClearLine()
    {
        foreach (var seg in segments)
            if (seg != null) Destroy(seg);
        segments.Clear();
    }
}