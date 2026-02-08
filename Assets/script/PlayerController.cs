using UnityEngine;
using UnityEngine.UI;
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
    private float verticalVelocity;

    [Header("--- 시점 회전 설정 ---")]
    public float mouseSensitivity = 100f;
    private float xRotation = 0f;

    [Header("--- 산소 시스템 ---")]
    public float maxOxygen = 100f;
    public float currentOxygen;
    public Image oxygenBar;
    public Image blindOverlay;
    [HideInInspector] public bool isInBase = false;

    [Header("--- 청소 모드 설정 ---")]
    public float cleanDistance = 50f;
    public enum WaterMode { Strong, Mid, Weak }
    public WaterMode currentMode = WaterMode.Mid;
    public float[] brushSizes = { 0.15f, 0.07f, 0.02f };
    public float[] cleanSpeeds = { 0.5f, 1.0f, 3.5f };

    [Header("--- UI 관련 ---")]
    public TextMeshProUGUI nozzleStatusText;

    void Start()
    {
        mainCam = Camera.main;
        currentOxygen = maxOxygen;
        Cursor.lockState = CursorLockMode.Locked;
        UpdateNozzleUI();
    }

    void Update()
    {
        // UI가 열려있을 때는 조작 방지 (나중에 N, B키 UI용)
        // if (Time.timeScale == 0) return; 

        HandleRotation();
        HandleMovement();
        HandleOxygen();
        HandleInputs();
    }

    private void HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentMode = (WaterMode)(((int)currentMode + 1) % 3);
            UpdateNozzleUI();
        }
        if (Input.GetKeyDown(KeyCode.E)) TryPickupTrash();
        if (Input.GetMouseButton(0)) HandleCleaning();
    }

    private void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z);

        // [테스트용] 중력과 수직 이동을 다 빼고 수평 이동만 체크
        // 만약 이래도 안 움직이면 키보드 입력 설정(Input Manager) 문제입니다.
        if (move.magnitude > 0.1f)
        {
            controller.Move(move * speed * Time.deltaTime);
        }

        // 스페이스바 상승 테스트
        if (Input.GetKey(KeyCode.Space))
        {
            controller.Move(Vector3.up * swimSpeed * Time.deltaTime);
        }
        else if (transform.position.y > minHeight)
        {
            controller.Move(Vector3.down * sinkSpeed * Time.deltaTime);
        }
    }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        mainCam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    private void TryPickupTrash()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        float detectionRadius = 1.5f;
        float maxDistance = 40f; // 요청하신 거리 40으로 수정

        int layerMask = 1 << LayerMask.NameToLayer("Trash");

        if (Physics.SphereCast(ray, detectionRadius, out hit, maxDistance, layerMask))
        {
            Trash trash = hit.collider.GetComponent<Trash>();
            if (trash == null) trash = hit.collider.GetComponentInParent<Trash>();

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
        string modeName = currentMode == WaterMode.Strong ? "광범위" : (currentMode == WaterMode.Mid ? "일반" : "집중");
        nozzleStatusText.text = "노즐 상태 : " + modeName;
    }

    private void HandleOxygen()
    {
        if (isInBase) currentOxygen += Time.deltaTime * 20f;
        else currentOxygen -= Time.deltaTime * 0.5f;

        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
        if (oxygenBar != null) oxygenBar.fillAmount = currentOxygen / maxOxygen;
    }

    private void OnTriggerEnter(Collider other) { if (other.CompareTag("Base")) isInBase = true; }
    private void OnTriggerExit(Collider other) { if (other.CompareTag("Base")) isInBase = false; }
}