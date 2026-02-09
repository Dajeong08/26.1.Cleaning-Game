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
        // 1. 기본 위치 페인팅
        DrawCircle(uv, radius, speed);

        // 2. 경계선 보정 (X축 0과 1이 만나는 지점 처리)
        // 브러시가 왼쪽 끝(0)에 걸치면 오른쪽 끝(1)도 같이 닦음
        if (uv.x < radius)
            DrawCircle(new Vector2(uv.x + 1.0f, uv.y), radius, speed);
        // 브러시가 오른쪽 끝(1)에 걸치면 왼쪽 끝(0)도 같이 닦음
        else if (uv.x > 1.0f - radius)
            DrawCircle(new Vector2(uv.x - 1.0f, uv.y), radius, speed);

        templateMask.Apply();

        if (Time.time >= nextUpdateTime)
        {
            UpdatePercentage();
            nextUpdateTime = Time.time + 0.15f;
        }
    }

    // 실제 원을 그리는 로직을 별도 함수로 분리
    private void DrawCircle(Vector2 uv, float radius, float speed)
    {
        int centerX = (int)(uv.x * templateMask.width);
        int centerY = (int)(uv.y * templateMask.height);
        int r = (int)(radius * templateMask.width);

        // 픽셀 범위 계산
        for (int x = -r; x < r; x++)
        {
            for (int y = -r; y < r; y++)
            {
                if (x * x + y * y < r * r)
                {
                    int px = centerX + x;
                    int py = centerY + y;

                    // 텍스처 범위를 벗어나는 픽셀은 무시 (DrawCircle을 여러번 호출하므로 중요)
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
        if (progressText == null) return;

        Color[] pixels = templateMask.GetPixels();
        float whitePixels = 0;

        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].r > 0.9f) whitePixels++; 
        }

        float rawRatio = whitePixels / pixels.Length;
        float finalProgress = (rawRatio / targetThreshold) * 100f;
        finalProgress = Mathf.Clamp(finalProgress, 0f, 100f);

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

    public void RevealDirt(float duration)
    {
        StartCoroutine(RevealRoutine(duration));
    }

    private IEnumerator RevealRoutine(float duration)
    {
        if (mat == null) mat = GetComponent<Renderer>().material;

        // 쉐이더의 _IsScanning 변수를 1로 켜기
        mat.SetFloat("_IsScanning", 1f);
        yield return new WaitForSeconds(duration);
        // 다시 0으로 끄기
        mat.SetFloat("_IsScanning", 0f);
    }
}