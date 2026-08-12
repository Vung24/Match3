using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };

    public enum eLevelMode
    {
        TIMER,
        MOVES,
        AUTO_WIN,
        AUTO_LOSE,
    }

    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER,
        GAME_WIN,
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set
        {
            m_state = value;

            StateChangedAction(m_state);
        }
    }


    private GameSettings m_gameSettings;

    private BoardController m_boardController;

    private BottomCell m_bottomCell;

    private UIMainManager m_uiMenu;

    private LevelCondition m_levelCondition;

    private bool m_isFinishing;

    private void Awake()
    {
        State = eStateGame.SETUP;

        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);

        m_uiMenu = FindObjectOfType<UIMainManager>();
        m_uiMenu.Setup(this);
    }

    void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_boardController != null) m_boardController.Update();
    }


    internal void SetState(eStateGame state)
    {
        State = state;

        if(State == eStateGame.PAUSE)
        {
            DOTween.PauseAll();
        }
        else
        {
            DOTween.PlayAll();
        }
    }

    public void LoadLevel(eLevelMode mode)
    {
        m_isFinishing = false;

        m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
        m_boardController.StartGame(this, m_gameSettings);

        m_levelCondition = CreateLevelCondition(mode);
        m_levelCondition.Setup(new LevelContext(
            this,
            m_boardController,
            m_gameSettings,
            m_uiMenu.GetLevelConditionView()));

        if (m_levelCondition.LoseWhenBottomCellsFilled)
            m_boardController.OnBottomCellsFilled += GameOver;

        m_levelCondition.ConditionWinEvent += GameWin;
        m_levelCondition.ConditionLoseEvent += GameOver;

        State = eStateGame.GAME_STARTED;
    }

    private LevelCondition CreateLevelCondition(eLevelMode mode)
    {
        return mode switch
        {
            eLevelMode.MOVES => gameObject.AddComponent<LevelMoves>(),
            eLevelMode.TIMER => gameObject.AddComponent<LevelTime>(),
            eLevelMode.AUTO_WIN => gameObject.AddComponent<LevelAutoWin>(),
            eLevelMode.AUTO_LOSE => gameObject.AddComponent<LevelAutoLose>(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported level mode")
        };
    }
    public void SetCurrentBottomCell(BottomCell bottomCell)
    {
        m_bottomCell = bottomCell;
    }
    public void GameOver()
    {
        if (m_isFinishing) return;

        m_isFinishing = true;
        m_boardController.FinishGame();
        StartCoroutine(WaitBoardController(false));
    }
    public void GameWin()
    {
        if (m_isFinishing) return;

        m_isFinishing = true;
        m_boardController.FinishGame();
        StartCoroutine(WaitBoardController(true));
    }

    internal void ClearLevel()
    {
        if (m_boardController)
        {
            m_boardController.OnBottomCellsFilled -= GameOver;
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            Destroy(m_bottomCell.gameObject);
            m_boardController = null;
            m_bottomCell = null;
        }

        m_isFinishing = false;
    }

    private IEnumerator WaitBoardController(bool isWin)
    {
        while (m_boardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);

        if (isWin)
            State = eStateGame.GAME_WIN;
        else
            State = eStateGame.GAME_OVER;

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionWinEvent -= GameWin;
            m_levelCondition.ConditionLoseEvent -= GameOver;

            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }
}
