using UnityEngine;

public class InventoryGrid : MonoBehaviour {
    [Range(1, 20)]
    public int rows = 5;
    [Range(1, 20)]
    public int columns = 5;

    // TODO: Make this a 2D array of InventorySlots
    public bool[,] cellStates = new bool[0, 0];

    // Resize state array if needed
    public void Refresh() {
        if (cellStates == null || cellStates.GetLength(0) != rows || cellStates.GetLength(1)!= columns) {
            bool[,] newStates = new bool[columns, rows];
            for (int x = 0; x < columns; x++) {
                for (int y = 0; y < rows; y++) {
                    if (x < cellStates.GetLength(0) && y < cellStates.GetLength(1)) {
                        newStates[x, y] = cellStates[x, y];
                    }
                    else {
                        newStates[x, y] = false; // Default to false for new cells
                    }
                }
            }
            cellStates = newStates;
        }
    }

    public void DisableAll() {
        SetAll(false);
    }

    public void EnableAll() { 
        SetAll(true);
    }

    private void SetAll(bool to) {
        for (int x = 0; x < cellStates.GetLength(0); x++) {
            for (int y = 0; y < cellStates.GetLength(1); y++) {
                cellStates[x, y] = to;
            }
        }
        Refresh();
    }
}
