using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SpriteCooldown : MonoBehaviour
{
    [Header("UI 설정")]
    public Image targetImage;           // 스프라이트가 교체될 UI Image
    public Sprite[] cooldownSprites;    // 0번(비어있음) ~ 6번(꽉 참) 총 7장
    public TextMeshProUGUI cooldownText; // 쿨타임 숫자를 표시할 텍스트 (TMP)

    [Header("스킬 설정")]
    public float skillCooldown = 3.0f;  // 해당 스킬의 쿨타임 시간 (인스펙터에서 설정)

    private bool isCooldown = false;

    void Start()
    {
        // 1. 처음 시작할 때 기술 사용 가능 상태 스프라이트 설정
        if (targetImage != null && cooldownSprites.Length > 0)
            targetImage.sprite = cooldownSprites[cooldownSprites.Length - 1];

        // 2. 시작 시 텍스트에 초기 쿨타임 표시
        UpdateInitialText();
    }

    // 초기 상태(대기 상태) 텍스트를 설정하는 함수
    private void UpdateInitialText()
    {
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(true); // 항상 보이게 설정
            cooldownText.text = skillCooldown.ToString("F1"); // 인스펙터에 설정한 초 표시
            cooldownText.color = Color.white; // 대기 중일 때는 흰색 (원하는 색으로 변경 가능)
        }
    }

    // 기술 사용 시 호출 (AbilityManager에서 호출)
    public void StartCooldown(float duration)
    {
        // 인스펙터 설정값과 동기화 (혹시 다를 경우를 대비)
        skillCooldown = duration;

        if (!isCooldown)
        {
            StartCoroutine(CooldownRoutine(duration));
        }
    }

    IEnumerator CooldownRoutine(float duration)
    {
        isCooldown = true;

        if (cooldownText != null)
        {
            cooldownText.color = Color.white; // 줄어들 때는 색상을 바꿔서 강조 (선택 사항)
        }

        targetImage.sprite = cooldownSprites[0];

        float elapsed = 0f;
        int lastIndex = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remainingTime = duration - elapsed;

            // 1. 스프라이트 교체 로직
            int currentIndex = Mathf.FloorToInt((elapsed / duration) * (cooldownSprites.Length - 1));
            if (currentIndex != lastIndex)
            {
                targetImage.sprite = cooldownSprites[currentIndex];
                lastIndex = currentIndex;
            }

            // 2. 남은 시간 실시간 표시
            if (cooldownText != null)
            {
                // 소수점 한자리까지 표시 (남은 시간이 0보다 작아지지 않게 Clamp)
                cooldownText.text = Mathf.Max(0, remainingTime).ToString("F1");
            }

            yield return null;
        }

        // 3. 쿨타임 종료 세팅
        targetImage.sprite = cooldownSprites[cooldownSprites.Length - 1];
        isCooldown = false;

        // 4. 종료 후 다시 초기 쿨타임 숫자로 복구
        UpdateInitialText();

        Debug.Log($"{gameObject.name} 쿨타임 완료!");
    }

    public bool IsOnCooldown() => isCooldown;
}