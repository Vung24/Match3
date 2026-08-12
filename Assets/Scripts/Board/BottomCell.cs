using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BottomCell : MonoBehaviour
{
    private int cellSize;
    private int boardSizeY;
    public List<Cell> cells = new List<Cell>();
    public Vector2 CreateBottomCell(GameSettings gameSettings)
    {
        cellSize = gameSettings.BottomCellSize;
        boardSizeY = gameSettings.BoardSizeY;

        Vector3 origin = new Vector3(-cellSize * 0.5f + 0.5f, -boardSizeY * 0.5f - 1f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < cellSize; x++)
        {
            GameObject go = GameObject.Instantiate(prefabBG);
            go.transform.position = origin + new Vector3(x, 0f, 0f);
            go.transform.SetParent(transform);
            cells.Add(go.GetComponent<Cell>());
            go.GetComponent<Cell>().isBelongBoard = false;
        }
        return origin;
    }
}
