using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

[System.Serializable]
public class LevelGoal
{
    public MicrobeType type;
    public int targetAmount = 5;
    public string labelName = "";
}

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int width = 5;
    public int height = 5;
    public float tileSize = 80f;
    public float spacing = 6f;
    public float boardScale = 1f;

    [Header("Responsive Layout")]
    public bool useResponsiveLayout = true;
    public float maxBoardWidthPercent = 0.82f;
    public float maxBoardHeightPercent = 0.42f;
    public float spacingPercent = 0.08f;
    public float boardYOffset = 190f;

    [Header("Board Parent")]
    public RectTransform boardParent;

    [Header("Tile")]
    public GameObject tilePrefab;

    [Header("Sprites")]
    public Sprite yellowSprite;
    public Sprite redSprite;
    public Sprite blueSprite;
    public Sprite greenSprite;

    [Header("Level Colors")]
    public MicrobeType[] availableMicrobes =
    {
        MicrobeType.Yellow,
        MicrobeType.Red,
        MicrobeType.Blue,
        MicrobeType.Green
    };

    [Header("Level Goals")]
    public LevelGoal[] goals;

    [Header("Goal UI")]
    public Text goalTextPrefab;
    public RectTransform goalTextParent;
    public float goalTextSpacing = 55f;
    public float goalDistanceUnderBoard = 85f;
    public int goalFontSize = 36;

    [Header("Audio")]
    public AudioClip[] matchSounds;
    public float soundFadeOutDuration = 0.35f;

    [Header("Effects")]
    public GameObject explosionPrefab;
    public float explosionDuration = 0.45f;
    public float explosionSizeMultiplier = 2.5f;
    public VideoClip explosionVideoClip;

    [Header("Level Complete Video")]
    public GameObject levelCompleteVideoPrefab;
    public RectTransform videoParent;
    public float videoFadeDuration = 0.35f;
    public float videoFallbackWait = 5f;

    [Header("Level")]
    public string nextSceneName = "Level2";

    private MicrobeTile[,] board;
    private Dictionary<MicrobeType, Sprite> sprites;
    private Dictionary<MicrobeType, int> collected = new Dictionary<MicrobeType, int>();
    private Dictionary<MicrobeType, Text> goalTexts = new Dictionary<MicrobeType, Text>();

    private bool isBusy;
    private bool levelCompleted;

    private Vector2 dragStartPosition;
    private int selectedRow = -1;
    private int selectedColumn = -1;

    private class MatchGroup
    {
        public MicrobeType type;
        public List<Vector2Int> cells = new List<Vector2Int>();
    }

    private void Start()
    {
        sprites = new Dictionary<MicrobeType, Sprite>
        {
            { MicrobeType.Yellow, yellowSprite },
            { MicrobeType.Red, redSprite },
            { MicrobeType.Blue, blueSprite },
            { MicrobeType.Green, greenSprite }
        };

        SetupResponsiveLayout();
        SetupGoals();
        CreateGoalTexts();
        CreateBoard();
        UpdateGoalUI();
    }

    private void Update()
    {
        if (isBusy || levelCompleted) return;
        HandleInput();
    }

    private void SetupVideoPlayer(VideoPlayer videoPlayer, VideoClip clip)
    {
        if (videoPlayer == null) return;

        videoPlayer.Stop();
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
    }

    private void SetupResponsiveLayout()
    {
        if (!useResponsiveLayout || boardParent == null) return;

        Canvas canvas = boardParent.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        float maxBoardWidth = canvasWidth * maxBoardWidthPercent;
        float maxBoardHeight = canvasHeight * maxBoardHeightPercent;

        float ratio = spacingPercent;

        float tileByWidth = maxBoardWidth / (width + ((width - 1) * ratio));
        float tileByHeight = maxBoardHeight / (height + ((height - 1) * ratio));

        tileSize = Mathf.Min(tileByWidth, tileByHeight);
        spacing = tileSize * ratio;
        boardScale = 1f;

        float totalBoardWidth = width * tileSize + (width - 1) * spacing;
        float totalBoardHeight = height * tileSize + (height - 1) * spacing;

        boardParent.anchorMin = new Vector2(0.5f, 0.5f);
        boardParent.anchorMax = new Vector2(0.5f, 0.5f);
        boardParent.pivot = new Vector2(0.5f, 0.5f);
        boardParent.sizeDelta = new Vector2(totalBoardWidth, totalBoardHeight);
        boardParent.anchoredPosition = new Vector2(0f, boardYOffset);
        boardParent.localScale = Vector3.one;

        SetupGoalParentPosition(totalBoardHeight);
    }

    private void SetupGoalParentPosition(float totalBoardHeight)
    {
        if (goalTextParent == null) return;

        goalTextParent.anchorMin = new Vector2(0.5f, 0.5f);
        goalTextParent.anchorMax = new Vector2(0.5f, 0.5f);
        goalTextParent.pivot = new Vector2(0.5f, 0.5f);

        float goalY = boardYOffset - totalBoardHeight / 2f - goalDistanceUnderBoard;

        goalTextParent.anchoredPosition = new Vector2(0f, goalY);
        goalTextParent.sizeDelta = new Vector2(750f, 200f);
        goalTextParent.localScale = Vector3.one;
    }

    private void SetupGoals()
    {
        collected.Clear();

        foreach (LevelGoal goal in goals)
        {
            if (!collected.ContainsKey(goal.type))
                collected.Add(goal.type, 0);
        }
    }

    private void CreateGoalTexts()
    {
        goalTexts.Clear();

        if (goalTextPrefab == null || goalTextParent == null) return;

        foreach (Transform child in goalTextParent)
        {
            if (child != goalTextPrefab.transform)
                Destroy(child.gameObject);
        }

        goalTextPrefab.gameObject.SetActive(false);

        float startY = ((goals.Length - 1) * goalTextSpacing) / 2f;

        for (int i = 0; i < goals.Length; i++)
        {
            Text newText = Instantiate(goalTextPrefab, goalTextParent);
            newText.gameObject.SetActive(true);

            newText.alignment = TextAnchor.MiddleCenter;
            newText.fontSize = goalFontSize;
            newText.resizeTextForBestFit = false;

            RectTransform rect = newText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(750f, 60f);
            rect.anchoredPosition = new Vector2(0f, startY - i * goalTextSpacing);
            rect.localScale = Vector3.one;

            if (!goalTexts.ContainsKey(goals[i].type))
                goalTexts.Add(goals[i].type, newText);
        }
    }

    private void CreateBoard()
    {
        board = new MicrobeTile[width, height];

        foreach (Transform child in boardParent)
        {
            if (child.GetComponent<MicrobeTile>() != null)
                Destroy(child.gameObject);
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                CreateTile(x, y, GetRandomType());
            }
        }
    }

    private void CreateTile(int x, int y, MicrobeType type)
    {
        GameObject tileObject = Instantiate(tilePrefab, boardParent);

        RectTransform rectTransform = tileObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(tileSize, tileSize);
        rectTransform.localScale = Vector3.one * boardScale;
        rectTransform.anchoredPosition = GetTilePosition(x, y);

        MicrobeTile tile = tileObject.GetComponent<MicrobeTile>();
        tile.Initialize(type, sprites[type]);

        board[x, y] = tile;
    }

    private Vector2 GetTilePosition(int x, int y)
    {
        float step = tileSize + spacing;

        float totalWidth = width * tileSize + (width - 1) * spacing;
        float totalHeight = height * tileSize + (height - 1) * spacing;

        float startX = -totalWidth / 2f + tileSize / 2f;
        float startY = totalHeight / 2f - tileSize / 2f;

        return new Vector2(startX + x * step, startY - y * step);
    }

    private MicrobeType GetRandomType()
    {
        if (availableMicrobes == null || availableMicrobes.Length == 0)
            return MicrobeType.Yellow;

        return availableMicrobes[Random.Range(0, availableMicrobes.Length)];
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
            StartDrag(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
            EndDrag(Input.mousePosition);
    }

    private void StartDrag(Vector2 screenPosition)
    {
        dragStartPosition = screenPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boardParent,
            screenPosition,
            null,
            out Vector2 localPoint
        );

        selectedColumn = GetColumn(localPoint);
        selectedRow = GetRow(localPoint);
    }

    private void EndDrag(Vector2 screenPosition)
    {
        if (selectedRow < 0 || selectedColumn < 0) return;

        Vector2 dragDelta = screenPosition - dragStartPosition;

        if (dragDelta.magnitude < 40f) return;

        if (Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y))
        {
            int direction = dragDelta.x > 0 ? 1 : -1;
            StartCoroutine(ShiftRowRoutine(selectedRow, direction));
        }
        else
        {
            int direction = dragDelta.y > 0 ? -1 : 1;
            StartCoroutine(ShiftColumnRoutine(selectedColumn, direction));
        }
    }

    private int GetColumn(Vector2 localPoint)
    {
        for (int x = 0; x < width; x++)
        {
            if (Mathf.Abs(localPoint.x - GetTilePosition(x, 0).x) <= tileSize * 0.6f)
                return x;
        }

        return -1;
    }

    private int GetRow(Vector2 localPoint)
    {
        for (int y = 0; y < height; y++)
        {
            if (Mathf.Abs(localPoint.y - GetTilePosition(0, y).y) <= tileSize * 0.6f)
                return y;
        }

        return -1;
    }

    private IEnumerator ShiftRowRoutine(int row, int direction)
    {
        isBusy = true;

        MicrobeTile[] oldRow = new MicrobeTile[width];

        for (int x = 0; x < width; x++)
            oldRow[x] = board[x, row];

        for (int x = 0; x < width; x++)
        {
            int newX = (x + direction + width) % width;
            board[newX, row] = oldRow[x];
        }

        yield return AnimateBoardRoutine();
        yield return ResolveMatchesRoutine();

        isBusy = false;
    }

    private IEnumerator ShiftColumnRoutine(int column, int direction)
    {
        isBusy = true;

        MicrobeTile[] oldColumn = new MicrobeTile[height];

        for (int y = 0; y < height; y++)
            oldColumn[y] = board[column, y];

        for (int y = 0; y < height; y++)
        {
            int newY = (y + direction + height) % height;
            board[column, newY] = oldColumn[y];
        }

        yield return AnimateBoardRoutine();
        yield return ResolveMatchesRoutine();

        isBusy = false;
    }

    private IEnumerator AnimateBoardRoutine()
    {
        float duration = 0.15f;
        float timer = 0f;

        Dictionary<RectTransform, Vector2> startPositions = new Dictionary<RectTransform, Vector2>();
        Dictionary<RectTransform, Vector2> targetPositions = new Dictionary<RectTransform, Vector2>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                RectTransform rectTransform = board[x, y].GetComponent<RectTransform>();
                startPositions[rectTransform] = rectTransform.anchoredPosition;
                targetPositions[rectTransform] = GetTilePosition(x, y);
            }
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            foreach (RectTransform rectTransform in startPositions.Keys)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(
                    startPositions[rectTransform],
                    targetPositions[rectTransform],
                    t
                );
            }

            yield return null;
        }

        foreach (RectTransform rectTransform in targetPositions.Keys)
            rectTransform.anchoredPosition = targetPositions[rectTransform];
    }

    private IEnumerator ResolveMatchesRoutine()
    {
        List<MatchGroup> matchGroups = FindMatchGroups();

        if (matchGroups.Count == 0)
        {
            CheckWin();
            yield break;
        }

        PlayRandomMatchSound();

        HashSet<Vector2Int> cellsToDestroy = new HashSet<Vector2Int>();

        foreach (MatchGroup group in matchGroups)
        {
            if (collected.ContainsKey(group.type))
                collected[group.type]++;

            foreach (Vector2Int cell in group.cells)
                cellsToDestroy.Add(cell);
        }

        foreach (Vector2Int cell in cellsToDestroy)
        {
            Vector2 explosionPosition = board[cell.x, cell.y]
                .GetComponent<RectTransform>()
                .anchoredPosition;

            Destroy(board[cell.x, cell.y].gameObject);

            CreateTile(cell.x, cell.y, GetRandomType());

            StartCoroutine(PlayExplosionEffect(explosionPosition));
        }

        UpdateGoalUI();

        yield return new WaitForSeconds(0.1f);

        yield return ResolveMatchesRoutine();
    }

    private List<MatchGroup> FindMatchGroups()
    {
        List<MatchGroup> groups = new List<MatchGroup>();

        for (int y = 0; y < height; y++)
        {
            int count = 1;

            for (int x = 1; x < width; x++)
            {
                if (board[x, y].Type == board[x - 1, y].Type)
                    count++;
                else
                {
                    AddHorizontalGroup(groups, x, y, count);
                    count = 1;
                }
            }

            AddHorizontalGroup(groups, width, y, count);
        }

        for (int x = 0; x < width; x++)
        {
            int count = 1;

            for (int y = 1; y < height; y++)
            {
                if (board[x, y].Type == board[x, y - 1].Type)
                    count++;
                else
                {
                    AddVerticalGroup(groups, x, y, count);
                    count = 1;
                }
            }

            AddVerticalGroup(groups, x, height, count);
        }

        return groups;
    }

    private void AddHorizontalGroup(List<MatchGroup> groups, int endX, int y, int count)
    {
        if (count < 3) return;

        MatchGroup group = new MatchGroup();
        group.type = board[endX - 1, y].Type;

        for (int i = 0; i < count; i++)
            group.cells.Add(new Vector2Int(endX - 1 - i, y));

        groups.Add(group);
    }

    private void AddVerticalGroup(List<MatchGroup> groups, int x, int endY, int count)
    {
        if (count < 3) return;

        MatchGroup group = new MatchGroup();
        group.type = board[x, endY - 1].Type;

        for (int i = 0; i < count; i++)
            group.cells.Add(new Vector2Int(x, endY - 1 - i));

        groups.Add(group);
    }

    private void UpdateGoalUI()
    {
        foreach (LevelGoal goal in goals)
        {
            if (!goalTexts.ContainsKey(goal.type)) continue;

            int current = collected.ContainsKey(goal.type) ? collected[goal.type] : 0;
            string label = string.IsNullOrEmpty(goal.labelName) ? goal.type.ToString().ToUpper() : goal.labelName;

            goalTexts[goal.type].text = label + ": " + current + "/" + goal.targetAmount;
        }
    }

    private void CheckWin()
    {
        if (levelCompleted) return;

        foreach (LevelGoal goal in goals)
        {
            int current = collected.ContainsKey(goal.type) ? collected[goal.type] : 0;

            if (current < goal.targetAmount)
                return;
        }

        StartCoroutine(LevelCompleteRoutine());
    }

    private IEnumerator LevelCompleteRoutine()
    {
        levelCompleted = true;
        isBusy = true;

        yield return PlayLevelCompleteVideo();

        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator PlayLevelCompleteVideo()
    {
        if (levelCompleteVideoPrefab == null)
        {
            yield return new WaitForSeconds(1f);
            yield break;
        }

        RectTransform parent = videoParent;

        if (parent == null)
        {
            Canvas canvas = boardParent.GetComponentInParent<Canvas>();
            if (canvas != null)
                parent = canvas.GetComponent<RectTransform>();
        }

        if (parent == null)
        {
            yield return new WaitForSeconds(1f);
            yield break;
        }

        GameObject videoObject = Instantiate(levelCompleteVideoPrefab, parent);
        videoObject.transform.SetAsLastSibling();

        RectTransform rect = videoObject.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        CanvasGroup canvasGroup = videoObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = videoObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;

        VideoPlayer videoPlayer = videoObject.GetComponentInChildren<VideoPlayer>(true);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.isLooping = false;
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.Prepare();

            float prepareTimer = 0f;

            while (!videoPlayer.isPrepared && prepareTimer < 3f)
            {
                prepareTimer += Time.deltaTime;
                yield return null;
            }

            videoPlayer.Play();
        }

        float fadeTimer = 0f;

        while (fadeTimer < videoFadeDuration)
        {
            fadeTimer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeTimer / videoFadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        if (videoPlayer != null)
        {
            float waitTimer = 0f;

            while (waitTimer < videoFallbackWait)
            {
                waitTimer += Time.deltaTime;

                if (videoPlayer.frame > 0 && !videoPlayer.isPlaying)
                    break;

                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(videoFallbackWait);
        }

        fadeTimer = 0f;

        while (fadeTimer < videoFadeDuration)
        {
            fadeTimer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeTimer / videoFadeDuration);
            yield return null;
        }

        Destroy(videoObject);
    }

    private void PlayRandomMatchSound()
    {
        if (matchSounds == null || matchSounds.Length == 0) return;

        AudioClip clip = matchSounds[Random.Range(0, matchSounds.Length)];
        StartCoroutine(PlaySoundWithFadeOut(clip, soundFadeOutDuration));
    }

    private IEnumerator PlaySoundWithFadeOut(AudioClip clip, float fadeOutDuration)
    {
        if (clip == null) yield break;

        GameObject soundObject = new GameObject("MatchSound_FadeOut");
        AudioSource source = soundObject.AddComponent<AudioSource>();

        source.clip = clip;
        source.volume = 1f;
        source.Play();

        float waitBeforeFade = clip.length - fadeOutDuration;

        if (waitBeforeFade > 0f)
            yield return new WaitForSeconds(waitBeforeFade);

        float timer = 0f;

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(1f, 0f, timer / fadeOutDuration);
            yield return null;
        }

        source.Stop();
        Destroy(soundObject);
    }

    private IEnumerator PlayExplosionEffect(Vector2 position)
    {
        if (explosionPrefab == null) yield break;

        GameObject effect = Instantiate(explosionPrefab, boardParent);
        effect.transform.SetAsLastSibling();

        RectTransform rect = effect.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(
                tileSize * explosionSizeMultiplier,
                tileSize * explosionSizeMultiplier
            );
            rect.localScale = Vector3.one;
        }

        VideoPlayer videoPlayer = effect.GetComponentInChildren<VideoPlayer>(true);

        if (videoPlayer != null && explosionVideoClip != null)
        {
            SetupVideoPlayer(videoPlayer, explosionVideoClip);
            videoPlayer.Prepare();

            float prepareTimer = 0f;

            while (!videoPlayer.isPrepared && prepareTimer < 1f)
            {
                prepareTimer += Time.deltaTime;
                yield return null;
            }

            videoPlayer.Play();
        }

        yield return new WaitForSeconds(explosionDuration);

        Destroy(effect);
    }
}
