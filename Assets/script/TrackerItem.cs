using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TrackerItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI allInfoText;
    [SerializeField] private Button rewardButton;

    private MissionData myData;

    public void Setup(MissionData data)
    {
        myData = data;
        if (rewardButton != null) rewardButton.gameObject.SetActive(false);

        // 초기 텍스트 설정
        UpdateMissionStatus(
            $"<b>{data.missionName}</b>\n남은 따개비 개수 : {data.remainingTrash}\n닦기 진행도 : 0.0%",
            false,
            data.rewardCoin
        );
    }

    // 인스펙터 OnClick에서 연결할 함수
    public void OnClickRewardButton()
    {
        if (MissionManager.Instance != null && myData != null)
        {
            MissionManager.Instance.ClaimReward(myData.missionName, myData.rewardCoin, this.gameObject);
        }
    }

    public void UpdateMissionStatus(string info, bool isComplete, int reward)
    {
        if (allInfoText != null) allInfoText.text = info;
        if (rewardButton != null) rewardButton.gameObject.SetActive(isComplete);

        if (isComplete)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}