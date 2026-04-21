using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체 상태를 관리하는 매니저.
/// - 게임 상태 전환 (Ready → Playing → Paused → GameOver)
/// - 전역 이벤트 시스템
/// - 타임 스케일 관리 (히트스톱 등)
/// </summary>
public class GameManager : Singleton<GameManager>
{
    // ── 상태 ──────────────────────────────────────────
    [Header("Debug")]
    [SerializeField] private Define.GameState _currentState = Define.GameState.Ready;

    public Define.GameState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState == value) return;

            var prev = _currentState;
            _currentState = value;
            OnGameStateChanged?.Invoke(prev, _currentState);
        }
    }

    public bool IsPlaying => _currentState == Define.GameState.Playing;

    // ── 이벤트 ────────────────────────────────────────
    /// <summary>게임 상태가 변경될 때 발생합니다. (이전 상태, 새 상태)</summary>
    public event Action<Define.GameState, Define.GameState> OnGameStateChanged;

    /// <summary>적이 처치될 때 발생합니다.</summary>
    public event Action<GameObject> OnEnemyKilled;

    /// <summary>플레이어가 사망할 때 발생합니다.</summary>
    public event Action OnPlayerDeath;

    // ── 초기화 ────────────────────────────────────────
    protected override void OnSingletonAwake()
    {
        // 프레임 레이트 고정 (액션 게임은 안정적인 프레임 중요)
        Application.targetFrameRate = 60;

        // 수직동기화 끄기 (프로파일링 시 정확한 fps 측정)
        QualitySettings.vSyncCount = 0;
    }

    private void Start()
    {
        StartGame();
    }

    // ══════════════════════════════════════════════════
    //  게임 상태 전환
    // ══════════════════════════════════════════════════

    /// <summary>게임을 시작합니다.</summary>
    public void StartGame()
    {
        Time.timeScale = 1f;
        CurrentState = Define.GameState.Playing;
    }

    /// <summary>게임을 일시 정지합니다.</summary>
    public void PauseGame()
    {
        if (CurrentState != Define.GameState.Playing) return;

        Time.timeScale = 0f;
        CurrentState = Define.GameState.Paused;
    }

    /// <summary>일시 정지를 해제합니다.</summary>
    public void ResumeGame()
    {
        if (CurrentState != Define.GameState.Paused) return;

        Time.timeScale = 1f;
        CurrentState = Define.GameState.Playing;
    }

    /// <summary>게임 오버 처리를 합니다.</summary>
    public void GameOver()
    {
        if (CurrentState == Define.GameState.GameOver) return;

        CurrentState = Define.GameState.GameOver;
        // 게임 오버 시에는 슬로우 모션 연출 후 정지
        /*Time.timeScale = 0.3f;*/
        // 타임스케일은 GameUIManager.DeathSequence에서 관리
    }

    // ══════════════════════════════════════════════════
    //  이벤트 발행 (외부에서 호출)
    // ══════════════════════════════════════════════════

    /// <summary>적 처치 이벤트를 발행합니다.</summary>
    public void NotifyEnemyKilled(GameObject enemy)
    {
        OnEnemyKilled?.Invoke(enemy);
    }

    /// <summary>플레이어 사망 이벤트를 발행합니다.</summary>
    public void NotifyPlayerDeath()
    {
        OnPlayerDeath?.Invoke();
        GameOver();
    }

    // ══════════════════════════════════════════════════
    //  타임 스케일 유틸리티
    // ══════════════════════════════════════════════════

    private Coroutine _hitStopCoroutine;

    /// <summary>
    /// 히트스톱 연출. 지정 시간 동안 타임 스케일을 낮춥니다.
    /// 액션 게임의 타격감에 핵심적인 역할을 합니다.
    /// </summary>
    /// <param name="duration">히트스톱 지속 시간 (실제 시간)</param>
    /// <param name="timeScale">히트스톱 중 타임 스케일 (0.0 ~ 1.0)</param>
    public void HitStop(float duration = 0.1f, float timeScale = 0.05f)
    {
        if (_hitStopCoroutine != null)
            StopCoroutine(_hitStopCoroutine);

        _hitStopCoroutine = StartCoroutine(HitStopRoutine(duration, timeScale));
    }

    private System.Collections.IEnumerator HitStopRoutine(float duration, float scale)
    {
        Time.timeScale = scale;

        // WaitForSecondsRealtime: timeScale에 영향 받지 않음
        yield return new WaitForSecondsRealtime(duration);

        // 일시정지 상태가 아닐 때만 복구
        if (CurrentState != Define.GameState.Paused)
            Time.timeScale = 1f;

        _hitStopCoroutine = null;
    }

    // ══════════════════════════════════════════════════
    //  씬 관리
    // ══════════════════════════════════════════════════

    /// <summary>지정된 씬을 비동기 로드합니다.</summary>
    public void LoadScene(string sceneName)
    {
        // 씬 전환 전 이벤트 정리 (이전 씬 오브젝트의 stale 참조 방지)
        OnEnemyKilled = null;
        OnPlayerDeath = null;
        OnGameStateChanged = null;

        CurrentState = Define.GameState.Loading;
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(sceneName);
    }

    /// <summary>현재 씬을 다시 로드합니다. (리트라이)</summary>
    public void RestartScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }
}