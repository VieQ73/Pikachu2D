using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UILineDrawer : MonoBehaviour
{
    public Sprite lineSprite;
    public Color lineColor = Color.red;
    public float lineWidth = 5f;

    private readonly List<GameObject> segments = new List<GameObject>();

    public void DrawLine(List<Vector3> worldPoints, RectTransform canvasTransform, float duration, System.Action onComplete = null)
    {
        ClearLine();

        if (lineSprite == null)
        {
            Debug.LogError("[UILineDrawer] Chưa gán lineSprite!");
            return;
        }

        // Chuyển World → Local Canvas
        List<Vector3> localPoints = new List<Vector3>();
        foreach (var wp in worldPoints)
            localPoints.Add(canvasTransform.InverseTransformPoint(wp));

        // Tạo đoạn thẳng giữa các điểm
        for (int i = 0; i < localPoints.Count - 1; i++)
        {
            CreateSegment(localPoints[i], localPoints[i + 1]);
        }

        StartCoroutine(HideAfterDelay(duration, onComplete));
    }

    private void CreateSegment(Vector3 start, Vector3 end)
    {
        GameObject go = new GameObject("LineSegment", typeof(Image));
        go.transform.SetParent(transform, false);

        Image img = go.GetComponent<Image>();
        img.sprite = lineSprite;
        img.color = lineColor;
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.pivot = new Vector2(0, 0.5f);

        Vector3 dir = end - start;
        float length = dir.magnitude;

        rt.sizeDelta = new Vector2(length, lineWidth);
        rt.localPosition = start;
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
            Destroy(seg);
        segments.Clear();
    }
}
