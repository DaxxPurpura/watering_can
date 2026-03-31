using UnityEngine;
using System.Collections;

public class WateringCan_GrowFruit : MonoBehaviour
{
    void Start()
    {
        transform.localScale = Vector3.zero;
        StartCoroutine(Grow());
    }

    IEnumerator Grow()
    {
        float current = 0f;
        float duration = 0.5f;

        while (current < duration)
        {
            current += Time.deltaTime;
            float progress = Mathf.Clamp01(current / duration);
            float scale = 1f - Mathf.Pow(1f - progress, 3f);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        transform.localScale = Vector3.one;
    }
}