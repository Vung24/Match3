using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelTime : LevelCondition
{
    public override bool LoseWhenBottomCellsFilled => false;

    private float m_time;

    private GameManager m_mngr;

    private BoardController m_bctrl;

    public override void Setup(LevelContext context)
    {
        base.Setup(context);

        m_mngr = context.GameManager;

        m_bctrl = context.BoardController;
        m_bctrl.OnBottomCellTapped += OnBottomCellTapped;

        m_time = context.GameSettings.LevelTime;

        UpdateText();
    }

    private void Update()
    {
        if (m_conditionCompleted) return;

        if (m_mngr.State != GameManager.eStateGame.GAME_STARTED) return;

        m_time -= Time.deltaTime;

        if (m_time <= 0f)
        {
            m_time = 0f;
            UpdateText();
            m_bctrl.IsPlayerInputEnabled = false;
            OnConditionComplete(false);
            return;
        }

        UpdateText();
    }

    protected override void UpdateText()
    {
        m_txt.text = string.Format("TIME:\n{0:00}", m_time);
    }

    private void OnBottomCellTapped(Cell bottomCell)
    {
        m_bctrl.ReturnItemToInitialCell(bottomCell);
    }

    protected override void OnDestroy()
    {
        if (m_bctrl != null)
            m_bctrl.OnBottomCellTapped -= OnBottomCellTapped;

        base.OnDestroy();
    }
}
