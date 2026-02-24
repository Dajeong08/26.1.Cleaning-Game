using System.Collections;
using UnityEngine;
using TMPro;

public class DirtPainter : MonoBehaviour
{
    private Texture2D templateMask;
    private Material mat;
    private int textureSize = 512;

    [Header("UI 설정")]
    public TextMeshProUGUI progressText;

    [Header("정밀도 설정")]
    [Range(0.1f, 1f)]
    public float targetThreshold = 1f;

    private float nextUpdateTime;
    private float lastProgress = 0f; // 최적화를 위해 이전 진행도 저장

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        templateMask = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        Color[] startPixels = new Color[textureSize * textureSize];
        for (int i = 0; i < startPixels.Length; i++) startPixels[i] = Color.black;

        templateMask.SetPixels(startPixels);
        templateMask.Apply();
        mat.SetTexture("_MaskTex", templateMask);
    }

    public void Paint(Vector2 uv, float radius, float speed)
    {
        DrawCircle(uv, radius, speed);

        if (uv.x < radius)
            DrawCircle(new Vector2(uv.x + 1.0f, uv.y), radius, speed);
        else if (uv.x > 1.0f - radius)
            DrawCircle(new Vector2(uv.x - 1.0f, uv.y), radius, speed);

        templateMask.Apply();

        // 0.15초마다 진행도 계산 (퍼포먼스 확보)
        if (Time.time >= nextUpdateTime)
        {
            UpdatePercentage();
            nextUpdateTime = Time.time + 0.15f;
        }
    }

    private void DrawCircle(Vector2 uv, float radius, float speed)
    {
        int centerX = (int)(uv.x * templateMask.width);
        int centerY = (int)(uv.y * templateMask.height);
        int r = (int)(radius * templateMask.width);

        for (int x = -r; x < r; x++)
        {
            for (int y = -r; y < r; y++)
            {
                if (x * x + y * y < r * r)
                {
                    int px = centerX + x;
                    int py = centerY + y;

                    if (px < 0 || px >= templateMask.width || py < 0 || py >= templateMask.height)
                        continue;

                    Color currentColor = templateMask.GetPixel(px, py);
                    float newVal = Mathf.Clamp01(currentColor.r + (speed * Time.deltaTime));
                    templateMask.SetPixel(px, py, new Color(newVal, newVal, newVal, 1f));
                }
            }
        }
    }

    void UpdatePercentage()
    {
        Color[] pixels = templateMask.GetPixels();
        float whitePixels = 0;

        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].r > 0.9f) whitePixels++;
        }

        float rawRatio = whitePixels / pixels.Length;
        float finalProgress = (rawRatio / targetThreshold) * 100f;
        finalProgress = Mathf.Clamp(finalProgress, 0f, 100f);

        // --- ★ MissionManager 연동 부분 ★ ---
        // 진행도에 변화가 있을 때만 MissionManager 호출
        if (Mathf.Abs(lastProgress - finalProgress) > 0.1f)
        {
            lastProgress = finalProgress;
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.UpdateProgress(finalProgress);
            }
        }

        // 자체 UI 텍스트 업데이트
        if (progressText != null)
        {
            if (finalProgress >= 99.9f)
            {
                progressText.text = "미션 완료: 100%";
                progressText.color = Color.green;
            }
            else
            {
                progressText.text = $"청소 진행도: {finalProgress:F1}%";
                progressText.color = Color.white;
            }
        }
    }

    public void RevealDirt(float duration)
    {
        StartCoroutine(RevealRoutine(duration));
    }

    private IEnumerator RevealRoutine(float duration)
    {
        if (mat == null) mat = GetComponent<Renderer>().material;
        mat.SetFloat("_IsScanning", 1f);
        yield return new WaitForSeconds(duration);
        mat.SetFloat("_IsScanning", 0f);
    }
}