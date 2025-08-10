using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class LineDrawer : MonoBehaviour
{
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true; // Quan trọng để vẽ trong Game View
    }

    public void DrawLineAnimated(List<Vector3> worldPoints, float duration, System.Action onComplete = null)
    {
        if (worldPoints == null || worldPoints.Count < 2)
        {
            Debug.LogWarning("[LineDrawer] Không thể vẽ đường với ít hơn 2 điểm.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(DrawLineRoutine(worldPoints, duration, onComplete));
    }

    private IEnumerator DrawLineRoutine(List<Vector3> worldPoints, float duration, System.Action onComplete)
    {
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = true;

        float totalDist = 0;
        for (int i = 1; i < worldPoints.Count; i++)
            totalDist += Vector3.Distance(worldPoints[i - 1], worldPoints[i]);

        Vector3 prevPos = worldPoints[0];
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, prevPos);

        for (int i = 1; i < worldPoints.Count; i++)
        {
            Vector3 targetPos = worldPoints[i];
            float segDist = Vector3.Distance(prevPos, targetPos);
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / (duration * (segDist / totalDist));
                Vector3 newPos = Vector3.Lerp(prevPos, targetPos, t);
                if (lineRenderer.positionCount < i + 1)
                    lineRenderer.positionCount = i + 1;
                lineRenderer.SetPosition(i, newPos);
                yield return null;
            }
            prevPos = targetPos;
        }

        yield return new WaitForSeconds(0.1f);

        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;

        onComplete?.Invoke();
    }
}
