using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelContext
{
    public GameManager GameManager { get; }
    public BoardController BoardController { get; }
    public GameSettings GameSettings { get; }
    public Text ConditionView { get; }

    public LevelContext(
        GameManager gameManager,
        BoardController boardController,
        GameSettings gameSettings,
        Text conditionView)
    {
        GameManager = gameManager;
        BoardController = boardController;
        GameSettings = gameSettings;
        ConditionView = conditionView;
    }
}

public abstract class LevelCondition : MonoBehaviour
{
    public event Action ConditionWinEvent = delegate { };
    public event Action ConditionLoseEvent = delegate { };

    protected Text m_txt;

    protected bool m_conditionCompleted = false;

    public virtual bool LoseWhenBottomCellsFilled => true;

    public virtual void Setup(LevelContext context)
    {
        m_txt = context.ConditionView;
    }

    protected virtual void UpdateText() { }

    public void OnConditionComplete(bool isWin)
    {
        if (m_conditionCompleted)
            return;

        m_conditionCompleted = true;
        if (isWin) ConditionWinEvent();
        else ConditionLoseEvent();
    }

    protected virtual void OnDestroy()
    {

    }
}
