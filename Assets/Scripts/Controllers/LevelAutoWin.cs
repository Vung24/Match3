using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelAutoWin : LevelCondition
{
    private BoardController m_mngr;

    public override void Setup(LevelContext context)
    {
        base.Setup(context);

        m_mngr = context.BoardController;
        UpdateText();
        RunAutoWin();
    }

    private void RunAutoWin()
    {
        m_mngr.IsPlayerInputEnabled = false;
        StartCoroutine(AutoWinCoroutine());
    }

    private IEnumerator AutoWinCoroutine()
    {
        while (!m_mngr.IsBoardEmpty && !m_mngr.IsGameFinished)
        {
            yield return new WaitUntil(() => !m_mngr.IsBusy);
            yield return new WaitForSeconds(0.5f);

            if (m_mngr.IsBoardEmpty || m_mngr.IsGameFinished)
                yield break;

            if (m_mngr.HasItemsInBottomCells)
            {
                if (!m_mngr.MoveMatchingBoardItemToBottom())
                    m_mngr.MoveFirstBoardItemToBottom();
            }
            else
                m_mngr.MoveFirstBoardItemToBottom();
        }
    }

    protected override void UpdateText()
    {
        m_txt.text = string.Format("AUTO WIN");
    }
}
