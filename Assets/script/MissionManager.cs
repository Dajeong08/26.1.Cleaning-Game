using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("판넬 및 UI 설정")]
    public GameObject availablePanel;
    public GameObject completedPanel;
    public GameObject mMenuPanel;
    public Transform completedContent;

    [Header("보상 금액 설정")]
    public int tutorialReward = 200;   // 튜토리얼 보상 (인스펙터에서 수정 가능)
    public int submarineReward = 1000; // 잠수함 보상 (인스펙터에서 수정 가능)

    [Header("트래커(왼쪽 리스트) 설정")]
    public GameObject trackerPrefab;
    public Transform trackerParent;

    private Dictionary<string, TrackerItem> activeTrackers = new Dictionary<string, TrackerItem>();
    private Dictionary<string, MissionData> missionDatas = new Dictionary<string, MissionData>();

    [Header("미션 UI 카드")]
    public GameObject tutorialJobCard;
    public GameObject submarineJobCard;

    [Header("실제 맵 오브젝트")]
    public GameObject tutorialMapGroup;
    public GameObject submarineMapGroup;

    public string currentActiveMissionName;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        ShowAvailableJobs();
        if (tutorialMapGroup != null) tutorialMapGroup.SetActive(false);
        if (submarineMapGroup != null) submarineMapGroup.SetActive(false);
    }

    public void ShowAvailableJobs()
    {
        if (availablePanel != null)
        {
            availablePanel.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(availablePanel.GetComponent<RectTransform>());
        }

        if (completedPanel != null)
        {
            completedPanel.SetActive(false);
        }

        Debug.Log("[Tab] '진행 가능' 탭으로 전환됨");
    }

    public void ShowCompletedTab()
    {
        if (availablePanel != null)
        {
            availablePanel.SetActive(false);
        }

        if (completedPanel != null)
        {
            completedPanel.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(completedPanel.GetComponent<RectTransform>());
        }

        Debug.Log("[Tab] '완료됨' 탭으로 전환됨");
    }

    public void HandleMissionClick(string mName, MissionData.MissionStatus mStatus)
    {
        if (mStatus == MissionData.MissionStatus.ReadyToMove)
        {
            AcceptMission(mName);
        }
    }

    private void AcceptMission(string mName)
    {
        if (activeTrackers.ContainsKey(mName))
        {
            StartMapTransition(mName);
            return;
        }

        MissionData newData = new MissionData();
        newData.missionName = mName;
        newData.status = MissionData.MissionStatus.InProgress;

        GameObject mapGroup = null;
        string lowerName = mName.ToLower();

        if (lowerName.Contains("tutorial"))
            mapGroup = tutorialMapGroup;
        else if (lowerName.Contains("newmap") || lowerName.Contains("sub"))
            mapGroup = submarineMapGroup;

        if (mapGroup != null)
        {
            Trash[] trashes = mapGroup.GetComponentsInChildren<Trash>(true);
            newData.totalTrash = trashes.Length;
            newData.remainingTrash = trashes.Length;

            // [수정] 위에서 설정한 보상 변수를 적용
            if (lowerName.Contains("tutorial"))
                newData.rewardCoin = tutorialReward;
            else
                newData.rewardCoin = submarineReward;
        }
        missionDatas[mName] = newData;

        GameObject tObj = Instantiate(trackerPrefab, trackerParent);
        tObj.SetActive(true);
        TrackerItem script = tObj.GetComponent<TrackerItem>();
        script.Setup(newData);

        activeTrackers[mName] = script;

        StartMapTransition(mName);
    }

    public void StartMapTransition(string mName)
    {
        currentActiveMissionName = mName;
        string lowerName = mName.ToLower();

        Debug.Log($"[MissionManager] 이동 시도하는 미션 이름: '{mName}'");

        if (tutorialMapGroup != null) tutorialMapGroup.SetActive(false);
        if (submarineMapGroup != null) submarineMapGroup.SetActive(false);

        if (lowerName.Contains("tutorial"))
        {
            if (tutorialMapGroup != null) tutorialMapGroup.SetActive(true);
            Debug.Log("Tutorial 맵 활성화 완료");
        }
        else if (lowerName.Contains("newmap") || lowerName.Contains("sub"))
        {
            if (submarineMapGroup != null) submarineMapGroup.SetActive(true);
            Debug.Log("Submarine (NewMap) 맵 활성화 완료");
        }

        if (mMenuPanel != null) mMenuPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnTrashPickedUp()
    {
        if (!string.IsNullOrEmpty(currentActiveMissionName))
            OnTrashPickedUp(currentActiveMissionName);
    }

    public void OnTrashPickedUp(string mName)
    {
        if (!missionDatas.ContainsKey(mName)) return;
        missionDatas[mName].remainingTrash--;
        UpdateSpecificTrackerUI(mName);
    }

    public void UpdateProgress(float progress)
    {
        if (!string.IsNullOrEmpty(currentActiveMissionName))
            UpdateProgress(currentActiveMissionName, progress);
    }

    public void UpdateProgress(string mName, float progress)
    {
        if (!missionDatas.ContainsKey(mName)) return;
        missionDatas[mName].currentProgress = progress;
        UpdateSpecificTrackerUI(mName);
    }

    private void UpdateSpecificTrackerUI(string mName)
    {
        if (!activeTrackers.ContainsKey(mName)) return;

        MissionData data = missionDatas[mName];
        string infoText = $"<b>{data.missionName}</b>\n" +
                          $"남은 쓰레기 개수 : {data.remainingTrash}\n" +
                          $"닦기 진행도 : {data.currentProgress:F1}%";

        bool isAllDone = (data.remainingTrash <= 0) && (data.currentProgress >= 99.9f);
        activeTrackers[mName].UpdateMissionStatus(infoText, isAllDone, data.rewardCoin);
    }

    public void ClaimReward(string mName, int rewardAmount, GameObject trackerUI)
    {
        if (CoinManager.instance != null)
        {
            CoinManager.instance.AddCoins(rewardAmount);
            MoveToCompleted(mName);

            if (activeTrackers.ContainsKey(mName)) activeTrackers.Remove(mName);
            if (trackerUI != null) Destroy(trackerUI);
            if (missionDatas.ContainsKey(mName)) missionDatas.Remove(mName);

            if (currentActiveMissionName == mName) currentActiveMissionName = "";
        }
    }

    public void MoveToCompleted(string mName)
    {
        string lowerName = mName.ToLower();
        GameObject cardTarget = null;

        if (lowerName.Contains("tutorial"))
            cardTarget = tutorialJobCard;
        else if (lowerName.Contains("newmap") || lowerName.Contains("sub"))
            cardTarget = submarineJobCard;

        if (cardTarget != null)
        {
            cardTarget.transform.SetParent(completedContent);
            cardTarget.transform.localScale = Vector3.one;

            JobItemUI itemUI = cardTarget.GetComponent<JobItemUI>();
            if (itemUI != null) itemUI.SetMissionCompleted();
        }
    }
}