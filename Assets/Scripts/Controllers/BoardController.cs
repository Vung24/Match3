using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };
    public event Action<Cell> OnBottomCellTapped = delegate { };
    public event Action OnBottomCellsFilled = delegate { };

    public bool IsBusy { get; private set; }
    public bool IsPlayerInputEnabled { get; set; } = true;

    public bool IsBoardEmpty => m_board != null && m_board.isEmptyCell();
    public bool IsGameFinished => m_gameOver;
    public bool HasItemsInBottomCells => listChoosedCell.Count > 0;

    private Board m_board;

    private BottomCell m_bottomCell;
    private Vector2 originBottom;
    private List<NormalItem> listChoosedCell = new List<NormalItem>();
    private List<Cell> initialCell = new List<Cell>();

    private GameManager m_gameManager;

    private bool m_isDragging;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;

    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;

    private bool m_isSubscribedToGameState;

    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;

        m_gameManager.StateChangedAction +=  OnGameStateChange;
        m_isSubscribedToGameState = true;

        m_cam = Camera.main;

        m_board = new Board(this, gameSettings);

        m_bottomCell = new GameObject("BottomCell").AddComponent<BottomCell>();
        originBottom = m_bottomCell.CreateBottomCell(gameSettings);
        gameManager.SetCurrentBottomCell(m_bottomCell);

        Fill();
    }

    private void Fill()
    {
        m_board.Fill();
        //FindMatchesAndCollapse();
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_WIN:
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                //StopHints()
                break;
        }
    }


    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy || !IsPlayerInputEnabled) return;
        if (!m_hintIsShown)
        {
            m_timeAfterFill += Time.deltaTime;
            if (m_timeAfterFill > m_gameSettings.TimeForHint)
            {
                m_timeAfterFill = 0f;
                //ShowHint();
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null && !m_isDragging)
            {
                m_isDragging = true;
                m_hitCollider = hit.collider;
                Cell choosedCell = m_hitCollider.GetComponent<Cell>();
                if (choosedCell == null)
                {
                    ResetRayCast();
                    return;
                }

                if(choosedCell.isBelongBoard)
                    AddItemToBottomCell(choosedCell);
                else
                    OnBottomCellTapped(choosedCell);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetRayCast();
        }

        //if (Input.GetMouseButton(0) && m_isDragging)
        //{
        //    var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        //    if (hit.collider != null)
        //    {
        //        if (m_hitCollider != null && m_hitCollider != hit.collider)
        //        {
        //            StopHints();

        //            Cell c1 = m_hitCollider.GetComponent<Cell>();
        //            Cell c2 = hit.collider.GetComponent<Cell>();
        //            if (AreItemsNeighbor(c1, c2))
        //            {
        //                IsBusy = true;
        //                SetSortingLayer(c1, c2);
        //                m_board.Swap(c1, c2, () =>
        //                {
        //                    FindMatchesAndCollapse(c1, c2);
        //                });

        //                ResetRayCast();
        //            }
        //        }
        //    }
        //    else
        //    {
        //        ResetRayCast();
        //    }
        //}
    }

    private bool AddItemToBottomCell(Cell cell)
    {
        if (IsBusy || cell == null || cell.Item is not NormalItem normalItem ||
            listChoosedCell.Count >= m_gameSettings.BottomCellSize)
            return false;

        IsBusy = true;
        OnMoveEvent();
        cell.Free();

        int insertIndex = -1;

        for (int i = listChoosedCell.Count - 1; i >= 0; i--)
        {
            if (listChoosedCell[i].ItemType == normalItem.ItemType)
            {
                insertIndex = i + 1;
                break;
            }
        }

        if (insertIndex == -1)
        {
            listChoosedCell.Add(normalItem);
            initialCell.Add(cell);
            insertIndex = listChoosedCell.Count - 1;
        }
        else
        {
            listChoosedCell.Insert(insertIndex, normalItem);
            initialCell.Insert(insertIndex, cell);
        }
        normalItem.SetSortingLayerHigher();
        RearrangeBottomCell();

        StartCoroutine(CheckMatch());
        return true;
    }

    public void ReturnItemToInitialCell(Cell bottomCell)
    {
        if (IsBusy || bottomCell == null || bottomCell.Item is not NormalItem normalItem)
            return;

        for(int i = 0; i < listChoosedCell.Count; i++)
        {
            if (listChoosedCell[i] == normalItem)
            {
                IsBusy = true;
                OnMoveEvent();
                listChoosedCell.RemoveAt(i);
                Cell cell = initialCell[i];
                initialCell.RemoveAt(i);
                normalItem.View.DOMove(cell.transform.position, 0.3f).OnComplete(() =>
                {
                    normalItem.SetViewRoot(transform);

                    cell.Assign(normalItem);
                    cell.ApplyItemPosition(false);
                    IsBusy = false;
                });
                RearrangeBottomCell();
                break;
            }
        }
    }

    private IEnumerator CheckMatch()
    {
        yield return new WaitForSeconds(0.4f);
        Dictionary<NormalItem.eNormalType, int> itemCount = new Dictionary<NormalItem.eNormalType, int>();
        foreach (var item in listChoosedCell)
        {
            if (itemCount.ContainsKey(item.ItemType))
            {
                itemCount[item.ItemType]++;
            }
            else
            {
                itemCount[item.ItemType] = 1;
            }
        }
        foreach (var pair in itemCount)
        {
            if (pair.Value >= m_gameSettings.MatchesMin)
            {
                RemoveMatchedItems(pair.Key);
                break;
            }
        }

        IsBusy = false;

        if (m_board.isEmptyCell())
        {
            m_gameManager.GameWin();
            yield break;
        }
        if(listChoosedCell.Count == m_gameSettings.BottomCellSize)
            OnBottomCellsFilled();
    }
    private void RemoveMatchedItems(NormalItem.eNormalType matchedType)
    {
        int removedCount = 0;

        for (int i = 0; i < listChoosedCell.Count && removedCount < m_gameSettings.MatchesMin; i++)
        {
            if (listChoosedCell[i].ItemType == matchedType)
            {
                NormalItem item = listChoosedCell[i];

                listChoosedCell.RemoveAt(i);
                initialCell.RemoveAt(i);
                item.ExplodeView();

                removedCount++;
                i--;
            }
        }

        RearrangeBottomCell();
        //StartCoroutine(ShiftDownItemsCoroutine());
    }

    private void RearrangeBottomCell()
    {
        foreach (Cell bottomCell in m_bottomCell.cells)
            bottomCell.Free();

        for (int i = 0; i < listChoosedCell.Count; i++)
        {
            NormalItem item = listChoosedCell[i];
            Vector3 targetPos = new Vector3(
                originBottom.x + i,
                originBottom.y,
                0f
            );
            item.View.DOMove(targetPos, 0.3f).OnComplete(() => { });
            item.SetViewRoot(m_bottomCell.transform);

            m_bottomCell.cells[i].Assign(item);
        }
    }
    private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    public bool MoveFirstBoardItemToBottom()
    {
        Cell cell = m_board.GetFirstCell();
        if (cell == null) return false;

        return AddItemToBottomCell(cell);
    }

    public bool MoveMatchingBoardItemToBottom()
    {
        if (listChoosedCell.Count == 0)
            return MoveFirstBoardItemToBottom();

        Cell cell = m_board.GetMatchingCell(listChoosedCell[listChoosedCell.Count - 1]);
        if (cell == null) return false;

        return AddItemToBottomCell(cell);
    }

    public bool MoveNonMatchingBoardItemToBottom()
    {
        HashSet<NormalItem.eNormalType> typesInBottomCells = new HashSet<NormalItem.eNormalType>(
            listChoosedCell.Select(item => item.ItemType));

        Cell cell = m_board.GetFirstCellExcludingTypes(typesInBottomCells);

        if (cell == null)
        {
            HashSet<NormalItem.eNormalType> typesThatWouldMatch = new HashSet<NormalItem.eNormalType>(
                listChoosedCell
                    .GroupBy(item => item.ItemType)
                    .Where(group => group.Count() >= m_gameSettings.MatchesMin - 1)
                    .Select(group => group.Key));

            cell = m_board.GetFirstCellExcludingTypes(typesThatWouldMatch);
        }

        if (cell == null) return false;

        return AddItemToBottomCell(cell);
    }

    public void FinishGame()
    {
        m_gameOver = true;
        IsPlayerInputEnabled = false;
    }
    private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        if (cell1.Item is BonusItem)
        {
            cell1.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else if (cell2.Item is BonusItem)
        {
            cell2.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else
        {
            List<Cell> cells1 = GetMatches(cell1);
            List<Cell> cells2 = GetMatches(cell2);

            List<Cell> matches = new List<Cell>();
            matches.AddRange(cells1);
            matches.AddRange(cells2);
            matches = matches.Distinct().ToList();

            if (matches.Count < m_gameSettings.MatchesMin)
            {
                m_board.Swap(cell1, cell2, () =>
                {
                    IsBusy = false;
                });
            }
            else
            {
                OnMoveEvent();

                CollapseMatches(matches, cell2);
            }
        }
    }

    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;

                m_timeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }

    private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count < m_gameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count < m_gameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }

    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if(matches.Count > m_gameSettings.MatchesMin)
        {
            m_board.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();

        yield return new WaitForSeconds(0.2f);

        m_board.FillGapsWithNewItems();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();

        yield return new WaitForSeconds(0.2f);

        m_board.Fill();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();

        yield return new WaitForSeconds(0.3f);

        FindMatchesAndCollapse();
    }


    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }

    internal void Clear()
    {
        UnsubscribeFromGameState();
        m_board.Clear();
    }

    private void OnDestroy()
    {
        UnsubscribeFromGameState();
    }

    private void UnsubscribeFromGameState()
    {
        if (!m_isSubscribedToGameState || m_gameManager == null)
            return;

        m_gameManager.StateChangedAction -= OnGameStateChange;
        m_isSubscribedToGameState = false;
    }

    private void ShowHint()
    {
        m_hintIsShown = true;
        foreach (var cell in m_potentialMatch)
        {
            cell.AnimateItemForHint();
        }
    }

    private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
        {
            cell.StopHintAnimation();
        }

        m_potentialMatch.Clear();
    }
}
