using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JobItemUI : MonoBehaviour
{
    [Header("설정")]
    public string missionName; // 인스펙터에서 Tutorial 또는 NewMap 적기
    public int unlockPrice = 100; // 수령 비용

    [Header("연결할 UI 요소들")]
    public Button actionButton;
    public TextMeshProUGUI buttonText;

    // 현재 버튼의 상태
    private MissionData.MissionStatus currentStatus = MissionData.MissionStatus.Locked;

    void Start()
    {
        // 초기 텍스트 설정
        UpdateUI();
        actionButton.onClick.AddListener(OnButtonClick);
    }

    public void OnButtonClick()
    {
        if (currentStatus == MissionData.MissionStatus.Locked)
        {
            // 1단계: 의뢰 수령
            if (CoinManager.instance != null && CoinManager.instance.currentCoins >= unlockPrice)
            {
                CoinManager.instance.SubtractCoins(unlockPrice);
                currentStatus = MissionData.MissionStatus.ReadyToMove;
                UpdateUI();
                Debug.Log($"{missionName} 수령 완료! 이제 이동 가능합니다.");
            }
            else
            {
                Debug.Log("코인이 부족합니다!");
            }
        }
        else if (currentStatus == MissionData.MissionStatus.ReadyToMove)
        {
            // 2단계: 실제 맵 이동
            Debug.Log($"{missionName} 현장으로 이동 시작!");
            MissionManager.Instance.HandleMissionClick(missionName, currentStatus);
        }
    }

    // 미션 완료 시 MissionManager에서 호출할 함수
    public void SetMissionCompleted()
    {
        currentStatus = MissionData.MissionStatus.Rewarded;
        UpdateUI();
    }

    private void UpdateUI()
    {
        switch (currentStatus)
        {
            case MissionData.MissionStatus.Locked:
                buttonText.text = $"{unlockPrice}G로 수령";
                break;
            case MissionData.MissionStatus.ReadyToMove:
                buttonText.text = "현장으로 이동";
                break;
            case MissionData.MissionStatus.Rewarded:
                buttonText.text = "미션 완료";
                actionButton.interactable = false; // 버튼 비활성화
                break;
        }
    }
}