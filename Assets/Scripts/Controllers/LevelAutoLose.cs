using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelAutoLose : LevelCondition
{
    private BoardController m_mngr;

    public override void Setup(LevelContext context)
    {
        base.Setup(context);

        m_mngr = context.BoardController;
        UpdateText();
        RunAutoLose();
    }
    private void RunAutoLose()
    {
        m_mngr.IsPlayerInputEnabled = false;
        StartCoroutine(AutoLoseCoroutine());
    }

    private IEnumerator AutoLoseCoroutine()
    {
        while (!m_mngr.IsBoardEmpty && !m_mngr.IsGameFinished)
        {
            yield return new WaitUntil(() => !m_mngr.IsBusy);
            yield return new WaitForSeconds(0.5f);

            if (m_mngr.IsBoardEmpty || m_mngr.IsGameFinished)
                yield break;

            m_mngr.MoveNonMatchingBoardItemToBottom();
        }
    }

    protected override void UpdateText()
    {
        m_txt.text = string.Format("AUTO LOSE");
    }
}
