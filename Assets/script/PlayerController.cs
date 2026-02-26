using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("--- 컴포넌트 참조 ---")]
    public CharacterController controller;
    public Transform playerBody;
    private Camera mainCam;

    [Header("--- 이동 및 수영 설정 ---")]
    public float speed = 55f;
    public float swimSpeed = 40f;
    public float sinkSpeed = 30f;
    public float minHeight = 1.5f; // 이 Y값 이하로는 절대 못 내려감

    [Header("--- 시점 회전 설정 ---")]
    public float mouseSensitivity = 100f;
    private float xRotation = 0f;

    [Header("--- 산소 시스템 ---")]
    public float maxOxygen = 100f;
    public float currentOxygen;
    public Image oxygenBar;
    [HideInInspector] public bool isInBase = false;

    [Header("--- UI 시스템 ---")]
    [SerializeField] private GameObject UpgradeScreen;
    public bool isUIOpen { get { return isUpgradeOpen || isMissionMenuOpen; } }
    private bool isUpgradeOpen = false;
    private bool isMissionMenuOpen = false;

    [Header("--- Status UI ---")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI finStatusText;
    [SerializeField] private TextMeshProUGUI finLevelText;
    [SerializeField] private TextMeshProUGUI oxygenLevelText;
    [SerializeField] private TextMeshProUGUI oxygenRankText;

    [Header("--- Upgrade Buttons ---")]
    [SerializeField] private Button buyFinBtn;
    [SerializeField] private TextMeshProUGUI buyFinBtnText;
    [SerializeField] private Button upFinBtn;
    [SerializeField] private TextMeshProUGUI upFinBtnText;
    [SerializeField] private Button upOxyCapBtn;
    [SerializeField] private TextMeshProUGUI upOxyCapBtnText;
    [SerializeField] private Button upOxyEffBtn;
    [SerializeField] private TextMeshProUGUI upOxyEffBtnText;

    [Header("--- 플레이어 스펙 ---")]
    public bool hasFins = false;
    public int finLevel = 1;
    public int oxygenCapLevel = 1;
    public int oxygenEffLevel = 1;

    private int currentBuyFinCost = 100;
    private int currentUpFinCost = 50;
    private int currentUpOxyCapCost = 50;
    private int currentUpOxyEffCost = 80;

    [Header("--- 청소 시스템 ---")]
    public float cleanDistance = 50f;
    public enum WaterMode { Strong, Mid, Weak }
    public WaterMode currentMode = WaterMode.Mid;
    public float[] brushSizes = { 0.15f, 0.07f, 0.02f };
    public float[] cleanSpeeds = { 1.5f, 2.5f, 5.5f };
    public TextMeshProUGUI nozzleStatusText;

    void Start()
    {
        mainCam = Camera.main;
        currentOxygen = maxOxygen;
        Cursor.lockState = CursorLockMode.Locked;
        UpdateNozzleUI();
        if (UpgradeScreen != null) UpgradeScreen.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N)) ToggleUpgrade();
        if (Input.GetKeyDown(KeyCode.M)) ToggleMissionMenu();

        isMissionMenuOpen = (MissionManager.Instance != null && MissionManager.Instance.mMenuPanel.activeSelf);
        if (isUIOpen) return;

        HandleRotation();
        HandleMovement();
        HandleOxygen();
        HandleInputs();
    }

    private void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float currentMoveSpeed = speed;
        if (hasFins && Input.GetKey(KeyCode.LeftShift)) currentMoveSpeed = speed * 1.4f;

        Vector3 move = (transform.right * x + transform.forward * z);
        if (move.magnitude > 0.1f)
            controller.Move(move * currentMoveSpeed * Time.deltaTime);

        float finalSwimSpeed = (hasFins && Input.GetKey(KeyCode.LeftShift)) ? swimSpeed * 1.4f : swimSpeed;

        // 1. 상승 (Space 입력 시)
        if (Input.GetKey(KeyCode.Space))
        {
            controller.Move(Vector3.up * finalSwimSpeed * Time.deltaTime);
        }
        // 2. 하강 (Space 입력이 없고, 현재 높이가 minHeight보다 높을 때만 가라앉음)
        else if (transform.position.y > minHeight)
        {
            controller.Move(Vector3.down * sinkSpeed * Time.deltaTime);
        }

        // 3. 강제 높이 고정 (만약 외부 힘으로 인해 minHeight 아래로 뚫고 내려갔을 경우 즉시 복구)
        if (transform.position.y < minHeight)
        {
            Vector3 targetPos = transform.position;
            targetPos.y = minHeight;
            transform.position = targetPos;
        }
    }

    private void ToggleMissionMenu()
    {
        if (MissionManager.Instance == null || MissionManager.Instance.mMenuPanel == null) return;
        GameObject menu = MissionManager.Instance.mMenuPanel;
        bool isActive = !menu.activeSelf;
        menu.SetActive(isActive);

        if (isActive)
        {
            if (isUpgradeOpen) ToggleUpgrade();
            MissionManager.Instance.ShowAvailableJobs();
            SetCursor(true);
        }
        else
        {
            SetCursor(false);
        }
    }

    void ToggleUpgrade()
    {
        isUpgradeOpen = !isUpgradeOpen;
        if (UpgradeScreen != null) UpgradeScreen.SetActive(isUpgradeOpen);
        if (isUpgradeOpen)
        {
            UpdateStatusUI();
            if (MissionManager.Instance != null) MissionManager.Instance.mMenuPanel.SetActive(false);
            SetCursor(true);
        }
        else
        {
            SetCursor(false);
        }
    }

    private void SetCursor(bool show)
    {
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }

    public void BuyFins() { if (!hasFins && CoinManager.instance.currentCoins >= currentBuyFinCost) { CoinManager.instance.SubtractCoins(currentBuyFinCost); hasFins = true; UpdateStatusUI(); } }
    public void UpgradeFinLevel() { if (hasFins && CoinManager.instance.currentCoins >= currentUpFinCost) { CoinManager.instance.SubtractCoins(currentUpFinCost); finLevel++; speed += 10f; swimSpeed += 7f; currentUpFinCost += 60; UpdateStatusUI(); } }
    public void UpgradeOxygenCapacity() { if (CoinManager.instance.currentCoins >= currentUpOxyCapCost) { CoinManager.instance.SubtractCoins(currentUpOxyCapCost); oxygenCapLevel++; maxOxygen += 25f; currentOxygen = maxOxygen; currentUpOxyCapCost += 40; UpdateStatusUI(); } }
    public void UpgradeOxygenEfficiency() { if (oxygenEffLevel < 4 && CoinManager.instance.currentCoins >= currentUpOxyEffCost) { CoinManager.instance.SubtractCoins(currentUpOxyEffCost); oxygenEffLevel++; currentUpOxyEffCost += 70; UpdateStatusUI(); } }

    public void UpdateStatusUI()
    {
        if (CoinManager.instance != null) coinText.text = $"현재 보유 코인: {CoinManager.instance.currentCoins}G";
        finStatusText.text = hasFins ? "이동 장비: 오리발 (달리기/대쉬 가능)" : "이동 장비: 맨발";
        finLevelText.text = hasFins ? $"오리발 레벨: Lv.{finLevel}" : "오리발 레벨: 미획득";
        oxygenLevelText.text = $"산소통 레벨: Lv.{oxygenCapLevel} (최대 {maxOxygen:F0})";
        oxygenRankText.text = $"산소통 등급: {GetOxygenRankName(oxygenEffLevel)}";
        buyFinBtn.interactable = !hasFins && (CoinManager.instance.currentCoins >= currentBuyFinCost);
        buyFinBtnText.text = hasFins ? "획득 완료" : $"구매 ({currentBuyFinCost}G)";
        upFinBtn.interactable = hasFins && (CoinManager.instance.currentCoins >= currentUpFinCost);
        upFinBtnText.text = !hasFins ? "오리발 필요" : $"강화 ({currentUpFinCost}G)";
        upOxyCapBtn.interactable = (CoinManager.instance.currentCoins >= currentUpOxyCapCost);
        upOxyCapBtnText.text = $"확장 ({currentUpOxyCapCost}G)";
        if (oxygenEffLevel >= 4) { upOxyEffBtn.interactable = false; upOxyEffBtnText.text = "최대"; }
        else { upOxyEffBtn.interactable = (CoinManager.instance.currentCoins >= currentUpOxyEffCost); upOxyEffBtnText.text = $"강화 ({currentUpOxyEffCost}G)"; }
    }

    string GetOxygenRankName(int level) { switch (level) { case 1: return "일반"; case 2: return "강화"; case 3: return "전문가"; default: return "심해용"; } }

    private void HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Q)) { currentMode = (WaterMode)(((int)currentMode + 1) % 3); UpdateNozzleUI(); }
        if (Input.GetKeyDown(KeyCode.E)) TryPickupTrash();
        if (Input.GetMouseButton(0)) HandleCleaning();
    }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY; xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        mainCam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    private void HandleOxygen()
    {
        if (isInBase) currentOxygen += Time.deltaTime * 20f;
        else { float rate = 0.5f * (1.1f - (oxygenEffLevel * 0.1f)); currentOxygen -= Time.deltaTime * rate; }
        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
        if (oxygenBar != null) oxygenBar.fillAmount = currentOxygen / maxOxygen;
    }

    private void TryPickupTrash()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        if (Physics.SphereCast(ray, 1.5f, out hit, 40f, 1 << LayerMask.NameToLayer("Trash")))
        {
            Trash trash = hit.collider.GetComponent<Trash>() ?? hit.collider.GetComponentInParent<Trash>();
            if (trash != null)
            {
                if (CoinManager.instance != null) CoinManager.instance.AddCoins(trash.scoreValue);
                if (MissionManager.Instance != null) MissionManager.Instance.OnTrashPickedUp();
                Destroy(trash.gameObject);
            }
        }
    }

    private void HandleCleaning()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, cleanDistance, 1 << LayerMask.NameToLayer("Dirt")))
        {
            DirtPainter painter = hit.collider.GetComponent<DirtPainter>();
            if (painter != null) painter.Paint(hit.textureCoord, brushSizes[(int)currentMode], cleanSpeeds[(int)currentMode]);
        }
    }

    private void UpdateNozzleUI() { if (nozzleStatusText != null) nozzleStatusText.text = "노즐 : " + (currentMode == WaterMode.Strong ? "광범위" : (currentMode == WaterMode.Mid ? "일반" : "집중")); }
    private void OnTriggerEnter(Collider other) { if (other.CompareTag("Base")) isInBase = true; }
    private void OnTriggerExit(Collider other) { if (other.CompareTag("Base")) isInBase = false; }
}