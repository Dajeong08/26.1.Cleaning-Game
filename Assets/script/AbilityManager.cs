using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [Header("참조")]
    public PlayerMovement playerMovement;
    public CharacterController controller;

    [Header("대쉬 설정")]
    public float dashForce = 100f;      // 순간 이동 힘
    public float dashDuration = 0.2f;

    [System.Serializable]
    public class Ability
    {
        public string name;
        public KeyCode key;
        public float cooldownTime;
        public SpriteCooldown ui; // 원형 쿨타임 스크립트 참조
    }

    [Header("기술 리스트 (0: Scan, 1: Dash 순서 권장)")]
    public List<Ability> abilities = new List<Ability>();

    void Update()
    {
        // UI가 열려있으면 기술 사용 불가
        if (playerMovement != null && playerMovement.isUIOpen) return;

        foreach (Ability ability in abilities)
        {
            if (Input.GetKeyDown(ability.key))
            {
                // 쿨타임 체크
                if (ability.ui != null && !ability.ui.IsOnCooldown())
                {
                    // 대쉬인 경우 오리발 체크 추가
                    if (ability.name == "Dash" && (playerMovement == null || !playerMovement.hasFins)) continue;

                    ExecuteAbility(ability);
                }
            }
        }
    }

    private void ExecuteAbility(Ability ability)
    {
        // UI 쿨타임 애니메이션 시작
        ability.ui.StartCooldown(ability.cooldownTime);

        switch (ability.name)
        {
            case "Scan":
                DoScan();
                break;
            case "Dash":
                StartCoroutine(DashRoutine());
                break;
        }
    }

    private void DoScan()
    {
        DirtPainter[] painters = Object.FindObjectsByType<DirtPainter>(FindObjectsSortMode.None);
        foreach (var p in painters) p.RevealDirt(1f);
        Debug.Log("스캔 완료");
    }

    IEnumerator DashRoutine()
    {
        float startTime = Time.time;

        // 1. 방향 설정: 카메라가 보는 방향 (수평으로만 가고 싶다면 y를 0으로)
        Vector3 dashDir = Camera.main.transform.forward;

        // 만약 위아래로 튀는 게 싫다면 아래 주석을 해제하세요
        // dashDir.y = 0; 
        // dashDir.Normalize();

        Debug.Log("대쉬 시작! 방향: " + dashDir);

        // 2. 대쉬 지속 시간 동안 루프
        while (Time.time < startTime + dashDuration)
        {
            // CharacterController.Move는 '프레임당 이동할 벡터'를 인자로 받습니다.
            // dashForce가 충분히 커야 합니다 (예: 100 이상 추천)
            controller.Move(dashDir * dashForce * Time.deltaTime);

            yield return null;
        }

        Debug.Log("대쉬 끝");
    }
}