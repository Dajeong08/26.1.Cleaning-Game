using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
    public float minHeight = 1.5f;

    [Header("--- 시점 회전 설정 ---")]
    public float mouseSensitivity = 100f;
    private float xRotation = 0f;

    [Header("--- 스킬 배율 및 쿨타임 (오리발 전용) ---")]
    private float runMultiplier = 1.4f;
    private float dashMultiplier = 2.8f;
    private float dashDuration = 0.2f;
    public float dashCooldown = 10f;      // 대쉬 쿨타임 10초
    private float lastDashTime = -10f;    // 마지막 대쉬 시간
    private bool isDashing = false;
    [SerializeField] private Image dashSlider; // 대쉬 쿨타임 UI

    [Header("--- 산소 시스템 ---")]
    public float maxOxygen = 100f;
    public float currentOxygen;
    public Image oxygenBar;
    [HideInInspector] public bool isInBase = false;

    [Header("--- UI 시스템 ---")]
    [SerializeField] private GameObject UpgradeScreen;
    private bool isUpgradeOpen = false;

    [Header("--- [왼쪽] Status 텍스트 ---")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI finStatusText;
    [SerializeField] private TextMeshProUGUI finLevelText;
    [SerializeField] private TextMeshProUGUI oxygenLevelText;
    [SerializeField] private TextMeshProUGUI oxygenRankText;

    [Header("--- [오른쪽] 버튼 및 텍스트 (자동 제어) ---")]
    [SerializeField] private Button buyFinBtn;
    [SerializeField] private TextMeshProUGUI buyFinBtnText;
    [SerializeField] private Button upFinBtn;
    [SerializeField] private TextMeshProUGUI upFinBtnText;
    [SerializeField] private Button upOxyCapBtn;
    [SerializeField] private TextMeshProUGUI upOxyCapBtnText;
    [SerializeField] private Button upOxyEffBtn;
    [SerializeField] private TextMeshProUGUI upOxyEffBtnText;

    [Header("--- 스펙 및 초기 가격 데이터 ---")]
    public bool hasFins = false;
    public int finLevel = 1;
    public int oxygenCapLevel = 1;
    public int oxygenEffLevel = 1;

    private int currentBuyFinCost = 100;
    private int currentUpFinCost = 50;
    private int currentUpOxyCapCost = 50;
    private int currentUpOxyEffCost = 80;

    [Header("--- 스캔 및 청소 시스템 ---")]
    public float scanCooldown = 3f;
    private float lastScanTime = -10f;
    public Image scanSlider;
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
        if (isUpgradeOpen) return;

        HandleRotation();
        HandleMovement();
        HandleOxygen();
        HandleInputs();
        UpdateCooldownUI();
    }

    private void UpdateCooldownUI()
    {
        if (scanSlider != null)
        {
            float progress = Mathf.Clamp01((Time.time - lastScanTime) / scanCooldown);
            scanSlider.fillAmount = progress;
        }

        if (dashSlider != null)
        {
            float progress = Mathf.Clamp01((Time.time - lastDashTime) / dashCooldown);
            dashSlider.fillAmount = progress;
        }
    }

    private void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float currentMoveSpeed = speed;

        if (hasFins)
        {
            // [대쉬] 우클릭 + 쿨타임 10초
            if (Input.GetMouseButtonDown(1) && !isDashing && Time.time >= lastDashTime + dashCooldown)
            {
                StartCoroutine(DashRoutine());
            }

            if (isDashing)
            {
                currentMoveSpeed = speed * dashMultiplier;
            }
            // [달리기] Shift
            else if (Input.GetKey(KeyCode.LeftShift))
            {
                currentMoveSpeed = speed * runMultiplier;
            }
        }

        Vector3 move = (transform.right * x + transform.forward * z);
        if (move.magnitude > 0.1f)
            controller.Move(move * currentMoveSpeed * Time.deltaTime);

        float finalSwimSpeed = (hasFins && Input.GetKey(KeyCode.LeftShift)) ? swimSpeed * runMultiplier : swimSpeed;

        if (Input.GetKey(KeyCode.Space))
            controller.Move(Vector3.up * finalSwimSpeed * Time.deltaTime);
        else if (transform.position.y > minHeight)
            controller.Move(Vector3.down * sinkSpeed * Time.deltaTime);
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        lastDashTime = Time.time;
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
    }

    // --- 업그레이드 함수부 ---
    public void BuyFins()
    {
        if (!hasFins && CoinManager.instance.currentCoins >= currentBuyFinCost)
        {
            CoinManager.instance.SubtractCoins(currentBuyFinCost);
            hasFins = true;
            UpdateStatusUI();
        }
    }

    public void UpgradeFinLevel()
    {
        if (hasFins && CoinManager.instance.currentCoins >= currentUpFinCost)
        {
            CoinManager.instance.SubtractCoins(currentUpFinCost);
            finLevel++;
            speed += 10f;
            swimSpeed += 7f;
            currentUpFinCost += 60;
            UpdateStatusUI();
        }
    }

    public void UpgradeOxygenCapacity()
    {
        if (CoinManager.instance.currentCoins >= currentUpOxyCapCost)
        {
            CoinManager.instance.SubtractCoins(currentUpOxyCapCost);
            oxygenCapLevel++;
            maxOxygen += 25f;
            currentOxygen = maxOxygen;
            currentUpOxyCapCost += 40;
            UpdateStatusUI();
        }
    }

    public void UpgradeOxygenEfficiency()
    {
        if (oxygenEffLevel < 4 && CoinManager.instance.currentCoins >= currentUpOxyEffCost)
        {
            CoinManager.instance.SubtractCoins(currentUpOxyEffCost);
            oxygenEffLevel++;
            currentUpOxyEffCost += 70;
            UpdateStatusUI();
        }
    }

    // --- UI 업데이트 ---
    public void UpdateStatusUI()
    {
        if (CoinManager.instance != null)
            coinText.text = $"현재 보유 코인: {CoinManager.instance.currentCoins}G";

        finStatusText.text = hasFins ? "이동 장비: 오리발 (달리기/대쉬 해방)" : "이동 장비: 맨발";
        finLevelText.text = hasFins ? $"오리발 레벨: Lv.{finLevel}" : "오리발 레벨: 미획득";
        oxygenLevelText.text = $"산소통 레벨: Lv.{oxygenCapLevel} (최대 {maxOxygen:F0})";
        oxygenRankText.text = $"산소통 등급: {GetOxygenRankName(oxygenEffLevel)}";

        buyFinBtn.interactable = !hasFins && (CoinManager.instance.currentCoins >= currentBuyFinCost);
        buyFinBtnText.text = hasFins ? "획득 완료" : $"오리발 구매 ({currentBuyFinCost}G)";

        upFinBtn.interactable = hasFins && (CoinManager.instance.currentCoins >= currentUpFinCost);
        upFinBtnText.text = !hasFins ? "오리발 필요" : $"속도 강화 ({currentUpFinCost}G)";

        upOxyCapBtn.interactable = (CoinManager.instance.currentCoins >= currentUpOxyCapCost);
        upOxyCapBtnText.text = $"용량 확장 ({currentUpOxyCapCost}G)";

        if (oxygenEffLevel >= 4) { upOxyEffBtn.interactable = false; upOxyEffBtnText.text = "최고 등급"; }
        else { upOxyEffBtn.interactable = (CoinManager.instance.currentCoins >= currentUpOxyEffCost); upOxyEffBtnText.text = $"등급 강화 ({currentUpOxyEffCost}G)"; }
    }

    string GetOxygenRankName(int level) { switch (level) { case 1: return "일반"; case 2: return "강화"; case 3: return "전문가용"; default: return "심해용"; } }

    void ToggleUpgrade()
    {
        isUpgradeOpen = !isUpgradeOpen;
        if (UpgradeScreen != null) { UpgradeScreen.SetActive(isUpgradeOpen); if (isUpgradeOpen) UpdateStatusUI(); }
        if (isUpgradeOpen) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; controller.Move(Vector3.zero); }
        else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    private void HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Q)) { currentMode = (WaterMode)(((int)currentMode + 1) % 3); UpdateNozzleUI(); }
        if (Input.GetKeyDown(KeyCode.E)) TryPickupTrash();
        if (Input.GetMouseButton(0)) HandleCleaning();
        if (Input.GetKeyDown(KeyCode.V) && Time.time >= lastScanTime + scanCooldown) ExecuteScan();
    }

    private void ExecuteScan() { lastScanTime = Time.time; DirtPainter[] painters = Object.FindObjectsByType<DirtPainter>(FindObjectsSortMode.None); foreach (var p in painters) p.RevealDirt(0.5f); }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        mainCam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    private void HandleOxygen()
    {
        if (isInBase) currentOxygen += Time.deltaTime * 20f;
        else
        {
            float consumptionRate = 0.5f * (1.1f - (oxygenEffLevel * 0.1f));
            currentOxygen -= Time.deltaTime * consumptionRate;
        }
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

    private void UpdateNozzleUI()
    {
        if (nozzleStatusText == null) return;
        nozzleStatusText.text = "노즐 상태 : " + (currentMode == WaterMode.Strong ? "광범위" : (currentMode == WaterMode.Mid ? "일반" : "집중"));
    }

    private void OnTriggerEnter(Collider other) { if (other.CompareTag("Base")) isInBase = true; }
    private void OnTriggerExit(Collider other) { if (other.CompareTag("Base")) isInBase = false; }
}