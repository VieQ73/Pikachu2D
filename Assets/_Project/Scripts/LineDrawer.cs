using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class LineDrawer : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _rectTransform = GetComponent<RectTransform>();
        _lineRenderer.enabled = false;
    }

    public void DrawLine(List<Vector2> points)
    {
        if (points == null || points.Count < 2) return;

        StopAllCoroutines(); // Dừng coroutine cũ nếu có

        _lineRenderer.positionCount = points.Count;
        Vector3[] positions = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            positions[i] = new Vector3(points[i].x, points[i].y, 0);
        }

        _lineRenderer.SetPositions(positions);
        _lineRenderer.enabled = true;

        StartCoroutine(HideLineAfterDelay(0.3f));
    }

    private IEnumerator HideLineAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _lineRenderer.enabled = false;
    }
}