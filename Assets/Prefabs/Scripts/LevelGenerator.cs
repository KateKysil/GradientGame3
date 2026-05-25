using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LevelGenerator : MonoBehaviour
{
    [Header("UI Панелі")]
    public GameObject menuPanel;
    public GameObject gamePanel;
    public GameObject winPanel;
    public GameObject losePanel; 
    public TextMeshProUGUI stepText;

    [Header("Генерація")]
    public GameObject tilePrefab;
    public Transform gridPanel;
    private GridLayoutGroup gridLayout;

    private int rows;
    private int cols;
    private List<Tile> allTiles = new List<Tile>();
    private Tile firstSelectedTile;

    private int stepCount = 0;
    private int maxSteps = 0; 

    private int currentSeed;
    private int currentMinSize, currentMaxSize;
    private float currentExtraFixedPercent;
    private int currentDifficulty;

    void Start()
    {
        gridLayout = gridPanel.GetComponent<GridLayoutGroup>();
        menuPanel.SetActive(true);
        gamePanel.SetActive(false);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
    }

    public void StartEasy() { StartGame(6, 12, 0.30f, 0, false); }
    public void StartMedium() { StartGame(9, 15, 0.15f, 1, false); }
    public void StartHard() { StartGame(12, 18, 0.0f, 2, false); }

    public void TryAgain()
    {
        StartGame(currentMinSize, currentMaxSize, currentExtraFixedPercent, currentDifficulty, true);
    }
    void StartGame(int minSize, int maxSize, float extraFixedPercent, int difficulty, bool isRestart)
    {
        menuPanel.SetActive(false);
        gamePanel.SetActive(true);
        winPanel.SetActive(false);
        losePanel.SetActive(false); 

        stepCount = 0;

        if (!isRestart)
        {
            currentSeed = Random.Range(0, int.MaxValue);

            currentMinSize = minSize;
            currentMaxSize = maxSize;
            currentExtraFixedPercent = extraFixedPercent;
            currentDifficulty = difficulty;
        }
        Random.InitState(currentSeed);
        cols = Random.Range(minSize, maxSize + 1);
        rows = Random.Range(minSize, maxSize + 1);

        if (gridLayout != null) gridLayout.constraintCount = cols;

        foreach (Transform child in gridPanel) Destroy(child.gameObject);
        allTiles.Clear();

        AdjustCellSize();

        int totalTiles = cols * rows;
        int fixedTilesCount = 4;
        if (difficulty == 0) fixedTilesCount += Mathf.RoundToInt((totalTiles - 4) * 0.30f);
        if (difficulty == 1) fixedTilesCount += Mathf.RoundToInt((totalTiles - 4) * 0.15f);

        int movableTilesCount = totalTiles - fixedTilesCount;
        float difficultyFactor = 1.3f;
        if (difficulty == 1) difficultyFactor = 2.0f; 
        if (difficulty == 2) difficultyFactor = 2.5f;

        maxSteps = Mathf.RoundToInt(movableTilesCount * difficultyFactor);
        UpdateStepText();

        Color[] levelPalette = GeneratePalette(difficulty);
        GenerateGrid(levelPalette);
        SetupExtraFixedTiles(extraFixedPercent);
        ShuffleTiles();
    }

    Color[] GeneratePalette(int difficulty)
    {
        Color[] palette = new Color[4];
        float baseHue = Random.Range(0f, 1f);
        float hueStep = difficulty == 0 ? 0.25f : (difficulty == 1 ? 0.12f : 0.05f);

        for (int i = 0; i < 4; i++)
        {
            float randomJitter = Random.Range(-0.02f, 0.02f);
            float h = Mathf.Repeat(baseHue + (hueStep * i) + randomJitter, 1f);

            if (difficulty == 0) palette[i] = Random.ColorHSV(h, h, 0.7f, 1f, 0.8f, 1f);
            else if (difficulty == 1) palette[i] = Random.ColorHSV(h, h, 0.5f, 0.8f, 0.7f, 0.9f);
            else palette[i] = Random.ColorHSV(h, h, 0.4f, 0.9f, 0.5f, 0.8f);
        }

        for (int i = 0; i < palette.Length; i++)
        {
            Color temp = palette[i];
            int randomIndex = Random.Range(i, palette.Length);
            palette[i] = palette[randomIndex];
            palette[randomIndex] = temp;
        }
        return palette;
    }

    void GenerateGrid(Color[] palette)
    {
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                GameObject newTile = Instantiate(tilePrefab, gridPanel);

                float u = (cols > 1) ? (float)x / (cols - 1) : 0f;
                float v = (rows > 1) ? (float)y / (rows - 1) : 0f;

                Color topColor = Color.Lerp(palette[0], palette[1], u);
                Color bottomColor = Color.Lerp(palette[2], palette[3], u);
                Color finalColor = Color.Lerp(topColor, bottomColor, v);

                Tile tileScript = newTile.GetComponent<Tile>();
                bool isCorner = (x == 0 && y == 0) || (x == cols - 1 && y == 0) || (x == 0 && y == rows - 1) || (x == cols - 1 && y == rows - 1);

                tileScript.Setup(new Vector2Int(x, y), finalColor, isCorner, this);
                allTiles.Add(tileScript);
            }
        }
    }

    void SetupExtraFixedTiles(float percentage)
    {
        if (percentage <= 0f) return;

        List<Tile> candidates = new List<Tile>();
        foreach (Tile t in allTiles)
        {
            if (!t.isFixed) candidates.Add(t);
        }

        int tilesToFix = Mathf.RoundToInt(candidates.Count * percentage);

        for (int i = 0; i < tilesToFix; i++)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            Tile t = candidates[randomIndex];
            t.isFixed = true;
            if (t.fixedIcon != null) t.fixedIcon.SetActive(true);
            candidates.RemoveAt(randomIndex);
        }
    }

    void ShuffleTiles()
    {
        List<Tile> movableTiles = new List<Tile>();
        foreach (Tile t in allTiles)
        {
            if (!t.isFixed) movableTiles.Add(t);
        }

        for (int i = 0; i < movableTiles.Count; i++)
        {
            int randomIndex = Random.Range(i, movableTiles.Count);
            movableTiles[i].SwapData(movableTiles[randomIndex]);
        }
    }
    public void TileClicked(Tile clickedTile)
    {
        if (winPanel.activeSelf || losePanel.activeSelf) return;

        if (firstSelectedTile == null)
        {
            firstSelectedTile = clickedTile;
            clickedTile.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        }
        else
        {
            if (firstSelectedTile == clickedTile)
            {
                firstSelectedTile.transform.localScale = Vector3.one;
                firstSelectedTile = null;
            }
            else
            {
                firstSelectedTile.SwapData(clickedTile);
                firstSelectedTile.transform.localScale = Vector3.one;
                firstSelectedTile = null;

                RegisterMove();
            }
        }
    }
    public void PerformDragSwap(Tile tileA, Tile tileB)
    {
        if (winPanel.activeSelf || losePanel.activeSelf) return;

        tileA.SwapData(tileB);
        if (firstSelectedTile != null)
        {
            firstSelectedTile.transform.localScale = Vector3.one;
            firstSelectedTile = null;
        }

        RegisterMove();
    }
    void RegisterMove()
    {
        stepCount++;
        UpdateStepText();
        if (CheckForWin())
        {
            return;
        }
        if (stepCount >= maxSteps)
        {
            if (losePanel != null) losePanel.SetActive(true);
        }
    }

    void UpdateStepText()
    {
        if (stepText != null)
        {
            stepText.text = $"Xоди: {stepCount} / {maxSteps}";
        }
    }
    bool CheckForWin()
    {
        foreach (Tile t in allTiles)
        {
            if (!t.IsInCorrectPosition()) return false;
        }

        if (winPanel != null) winPanel.SetActive(true);
        return true;
    }

    void AdjustCellSize()
    {
        if (gridLayout == null) return;
        RectTransform gridRect = gridPanel.GetComponent<RectTransform>();
        float panelWidth = gridRect.rect.width;
        float panelHeight = gridRect.rect.height;
        float paddingX = gridLayout.padding.left + gridLayout.padding.right;
        float paddingY = gridLayout.padding.top + gridLayout.padding.bottom;
        float spacingX = gridLayout.spacing.x * (cols - 1);
        float spacingY = gridLayout.spacing.y * (rows - 1);
        float availableWidth = panelWidth - paddingX - spacingX;
        float availableHeight = panelHeight - paddingY - spacingY;
        float maxCellWidth = availableWidth / cols;
        float maxCellHeight = availableHeight / rows;
        float finalCellSize = Mathf.Min(maxCellWidth, maxCellHeight);
        gridLayout.cellSize = new Vector2(finalCellSize, finalCellSize);
    }

    public void GoToMainMenu()
    {
        menuPanel.SetActive(true);
        gamePanel.SetActive(false);
        winPanel.SetActive(false);
        losePanel.SetActive(false);

        if (firstSelectedTile != null)
        {
            firstSelectedTile.transform.localScale = Vector3.one;
            firstSelectedTile = null;
        }
    }
}