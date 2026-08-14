using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class Game : MonoBehaviour
{
    public static Game Instance { get; private set; }

    public const int TOTAL_CHAPTERS = 20;
    public const int DAYS_PER_CHAPTER = 100;
    public const float DAY_LENGTH = 300f;

    private const string SAVE_FILE =
        "island100_save.json";

    // =========================================================
    // ENUMS
    // =========================================================

    public enum Language
    {
        Persian,
        English
    }

    public enum GameState
    {
        Playing,
        Paused,
        GameOver,
        ChapterComplete,
        Completed
    }

    public enum Weather
    {
        Sunny,
        Cloudy,
        Rain,
        Storm
    }

    // =========================================================
    // ITEM
    // =========================================================

    [Serializable]
    public class Item
    {
        public string id;
        public string name;
        public int amount;

        public Item()
        {
        }

        public Item(
            string id,
            string name,
            int amount)
        {
            this.id = id;
            this.name = name;
            this.amount = amount;
        }
    }

    // =========================================================
    // SAVE DATA
    // =========================================================

    [Serializable]
    public class SaveData
    {
        public int version = 1;

        public string playerId;

        public int chapter = 1;
        public int day = 1;

        public float dayTime;

        public GameState state =
            GameState.Playing;

        public Language language =
            Language.English;

        public Weather weather =
            Weather.Sunny;

        public float health = 100f;
        public float hunger = 100f;
        public float thirst = 100f;
        public float energy = 100f;

        public float playerX;
        public float playerY;
        public float playerZ;

        public List<Item> inventory =
            new List<Item>();

        public string[] chapterCodes =
            new string[TOTAL_CHAPTERS];

        public int wood;
        public int stone;
        public int food;
        public int water;

        public int enemiesDefeated;
        public int deaths;
    }

    // =========================================================
    // DATA
    // =========================================================

    public SaveData Data { get; private set; }

    private string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            SAVE_FILE
        );

    public int Chapter =>
        Data != null
            ? Data.chapter
            : 1;

    public int Day =>
        Data != null
            ? Data.day
            : 1;

    public float DayProgress =>
        Data == null
            ? 0f
            : Mathf.Clamp01(
                Data.dayTime /
                DAY_LENGTH
            );

    // =========================================================
    // EVENTS
    // =========================================================

    public event Action OnSave;
    public event Action OnLoad;

    public event Action<int, int>
        OnDayChanged;

    public event Action<int, string>
        OnChapterCompleted;

    public event Action
        OnGameOver;

    public event Action
        OnGameCompleted;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Load();
    }

    private void Update()
    {
        if (Data == null)
            return;

        if (Data.state !=
            GameState.Playing)
            return;

        UpdateTime();
        UpdateSurvival();
    }

    // =========================================================
    // NEW GAME
    // =========================================================

    public void NewGame()
    {
        Data =
            new SaveData();

        Data.playerId =
            GeneratePlayerId();

        Data.chapter = 1;
        Data.day = 1;
        Data.dayTime = 0f;

        Data.health = 100f;
        Data.hunger = 100f;
        Data.thirst = 100f;
        Data.energy = 100f;

        Data.state =
            GameState.Playing;

        Data.inventory =
            new List<Item>();

        Data.chapterCodes =
            new string[TOTAL_CHAPTERS];

        AddItem(
            "food",
            "Food",
            3
        );

        AddItem(
            "water",
            "Water",
            3
        );

        AddItem(
            "wood",
            "Wood",
            10
        );

        AddItem(
            "stone",
            "Stone",
            5
        );

        Save();
    }

    // =========================================================
    // TIME
    // =========================================================

    private void UpdateTime()
    {
        Data.dayTime +=
            Time.deltaTime;

        if (Data.dayTime <
            DAY_LENGTH)
            return;

        Data.dayTime = 0f;

        CompleteDay();
    }

    private void CompleteDay()
    {
        Data.day++;

        if (Data.day >
            DAYS_PER_CHAPTER)
        {
            CompleteChapter();
            return;
        }

        Data.energy =
            Mathf.Clamp(
                Data.energy + 10f,
                0f,
                100f
            );

        Save();

        OnDayChanged?.Invoke(
            Data.chapter,
            Data.day
        );
    }

    // =========================================================
    // CHAPTER
    // =========================================================

    private void CompleteChapter()
    {
        int completedChapter =
            Data.chapter;

        string code =
            GenerateChapterCode(
                completedChapter
            );

        Data.state =
            GameState.ChapterComplete;

        OnChapterCompleted?.Invoke(
            completedChapter,
            code
        );

        if (completedChapter >=
            TOTAL_CHAPTERS)
        {
            Data.state =
                GameState.Completed;

            OnGameCompleted?.Invoke();

            Save();

            return;
        }

        Data.chapter++;

        Data.day = 1;

        Data.dayTime = 0f;

        Data.state =
            GameState.Playing;

        Save();

        OnDayChanged?.Invoke(
            Data.chapter,
            Data.day
        );
    }

    // =========================================================
    // CHAPTER CODE
    // =========================================================

    private string GenerateChapterCode(
        int chapter)
    {
        if (chapter < 1 ||
            chapter > TOTAL_CHAPTERS)
            return "";

        if (!string.IsNullOrEmpty(
            Data.chapterCodes[
                chapter - 1
            ]))
        {
            return Data.chapterCodes[
                chapter - 1
            ];
        }

        string source =
            Data.playerId +
            "|" +
            chapter +
            "|ISLAND100";

        byte[] bytes =
            Encoding.UTF8.GetBytes(
                source
            );

        byte[] hash;

        using (
            SHA256 sha =
            SHA256.Create()
        )
        {
            hash =
                sha.ComputeHash(
                    bytes
                );
        }

        const string alphabet =
            "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        char[] chars =
            new char[12];

        for (int i = 0;
             i < chars.Length;
             i++)
        {
            chars[i] =
                alphabet[
                    hash[i] %
                    alphabet.Length
                ];
        }

        string code =
            $"{new string(chars, 0, 4)}-" +
            $"{new string(chars, 4, 4)}-" +
            $"{new string(chars, 8, 4)}";

        Data.chapterCodes[
            chapter - 1
        ] = code;

        return code;
    }

    public string GetChapterCode(
        int chapter)
    {
        if (Data == null)
            return "";

        if (chapter < 1 ||
            chapter > TOTAL_CHAPTERS)
            return "";

        return Data.chapterCodes[
            chapter - 1
        ];
    }

    // =========================================================
    // SURVIVAL
    // =========================================================

    private void UpdateSurvival()
    {
        Data.hunger -=
            Time.deltaTime * 0.01f;

        Data.thirst -=
            Time.deltaTime * 0.015f;

        Data.energy -=
            Time.deltaTime * 0.005f;

        Data.hunger =
            Mathf.Clamp(
                Data.hunger,
                0f,
                100f
            );

        Data.thirst =
            Mathf.Clamp(
                Data.thirst,
                0f,
                100f
            );

        Data.energy =
            Mathf.Clamp(
                Data.energy,
                0f,
                100f
            );

        if (Data.hunger <= 0f ||
            Data.thirst <= 0f)
        {
            Data.health -=
                Time.deltaTime * 0.02f;
        }

        Data.health =
            Mathf.Clamp(
                Data.health,
                0f,
                100f
            );

        if (Data.health <= 0f)
        {
            Die();
        }
    }

    public void Eat()
    {
        if (!RemoveItem(
            "food",
            1))
            return;

        Data.hunger =
            Mathf.Clamp(
                Data.hunger + 25f,
                0f,
                100f
            );
    }

    public void Drink()
    {
        if (!RemoveItem(
            "water",
            1))
            return;

        Data.thirst =
            Mathf.Clamp(
                Data.thirst + 30f,
                0f,
                100f
            );
    }

    public void Rest()
    {
        Data.energy =
            Mathf.Clamp(
                Data.energy + 25f,
                0f,
                100f
            );
    }

    public void Damage(
        float amount)
    {
        Data.health =
            Mathf.Clamp(
                Data.health - amount,
                0f,
                100f
            );

        if (Data.health <= 0f)
            Die();
    }

    public void Heal(
        float amount)
    {
        Data.health =
            Mathf.Clamp(
                Data.health + amount,
                0f,
                100f
            );
    }

    private void Die()
    {
        Data.deaths++;

        Data.state =
            GameState.GameOver;

        Save();

        OnGameOver?.Invoke();
    }

    // =========================================================
    // INVENTORY
    // =========================================================

    public void AddItem(
        string id,
        string name,
        int amount)
    {
        Item item =
            Data.inventory.Find(
                x => x.id == id
            );

        if (item != null)
        {
            item.amount += amount;
            return;
        }

        Data.inventory.Add(
            new Item(
                id,
                name,
                amount
            )
        );
    }

    public bool RemoveItem(
        string id,
        int amount)
    {
        Item item =
            Data.inventory.Find(
                x => x.id == id
            );

        if (item == null)
            return false;

        if (item.amount < amount)
            return false;

        item.amount -= amount;

        if (item.amount <= 0)
            Data.inventory.Remove(item);

        return true;
    }

    public int GetItemCount(
        string id)
    {
        Item item =
            Data.inventory.Find(
                x => x.id == id
            );

        return item == null
            ? 0
            : item.amount;
    }

    // =========================================================
    // RESOURCES
    // =========================================================

    public void CollectWood(
        int amount)
    {
        AddItem(
            "wood",
            "Wood",
            amount
        );

        Data.wood += amount;

        Save();
    }

    public void CollectStone(
        int amount)
    {
        AddItem(
            "stone",
            "Stone",
            amount
        );

        Data.stone += amount;

        Save();
    }

    public void CollectFood(
        int amount)
    {
        AddItem(
            "food",
            "Food",
            amount
        );

        Data.food += amount;

        Save();
    }

    public void CollectWater(
        int amount)
    {
        AddItem(
            "water",
            "Water",
            amount
        );

        Data.water += amount;

        Save();
    }

    // =========================================================
    // CRAFTING
    // =========================================================

    public bool CraftTool()
    {
        if (GetItemCount("wood") < 5)
            return false;

        if (GetItemCount("stone") < 3)
            return false;

        RemoveItem(
            "wood",
            5
        );

        RemoveItem(
            "stone",
            3
        );

        AddItem(
            "tool",
            "Tool",
            1
        );

        Save();

        return true;
    }

    public bool CraftWeapon()
    {
        if (GetItemCount("wood") < 8)
            return false;

        if (GetItemCount("stone") < 5)
            return false;

        RemoveItem(
            "wood",
            8
        );

        RemoveItem(
            "stone",
            5
        );

        AddItem(
            "weapon",
            "Weapon",
            1
        );

        Save();

        return true;
    }

    // =========================================================
    // LANGUAGE
    // =========================================================

    public void SetLanguage(
        Language language)
    {
        Data.language =
            language;

        Save();
    }

    public string Text(
        string english,
        string persian)
    {
        return Data.language ==
               Language.Persian
            ? persian
            : english;
    }

    // =========================================================
    // PAUSE
    // =========================================================

    public void Pause()
    {
        if (Data.state !=
            GameState.Playing)
            return;

        Data.state =
            GameState.Paused;

        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (Data.state !=
            GameState.Paused)
            return;

        Data.state =
            GameState.Playing;

        Time.timeScale = 1f;
    }

    // =========================================================
    // SAVE
    // =========================================================

    public void Save()
    {
        if (Data == null)
            return;

        try
        {
            string json =
                JsonUtility.ToJson(
                    Data,
                    true
                );

            File.WriteAllText(
                SavePath,
                json
            );

            OnSave?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Save failed: " +
                e.Message
            );
        }
    }

    // =========================================================
    // LOAD
    // =========================================================

    public void Load()
    {
        try
        {
            if (!File.Exists(
                SavePath))
            {
                NewGame();
                return;
            }

            string json =
                File.ReadAllText(
                    SavePath
                );

            Data =
                JsonUtility.FromJson<
                    SaveData
                >(json);

            if (Data == null)
            {
                NewGame();
                return;
            }

            ValidateData();

            OnLoad?.Invoke();
        }
        catch
        {
            NewGame();
        }
    }

    private void ValidateData()
    {
        Data.chapter =
            Mathf.Clamp(
                Data.chapter,
                1,
                TOTAL_CHAPTERS
            );

        Data.day =
            Mathf.Clamp(
                Data.day,
                1,
                DAYS_PER_CHAPTER
            );

        Data.dayTime =
            Mathf.Clamp(
                Data.dayTime,
                0f,
                DAY_LENGTH
            );

        Data.health =
            Mathf.Clamp(
                Data.health,
                0f,
                100f
            );

        Data.hunger =
            Mathf.Clamp(
                Data.hunger,
                0f,
                100f
            );

        Data.thirst =
            Mathf.Clamp(
                Data.thirst,
                0f,
                100f
            );

        Data.energy =
            Mathf.Clamp(
                Data.energy,
                0f,
                100f
            );

        if (Data.inventory == null)
        {
            Data.inventory =
                new List<Item>();
        }

        if (Data.chapterCodes == null ||
            Data.chapterCodes.Length !=
            TOTAL_CHAPTERS)
        {
            Data.chapterCodes =
                new string[
                    TOTAL_CHAPTERS
                ];
        }

        if (string.IsNullOrEmpty(
            Data.playerId))
        {
            Data.playerId =
                GeneratePlayerId();
        }
    }

    // =========================================================
    // PLAYER ID
    // =========================================================

    private string GeneratePlayerId()
    {
        byte[] bytes =
            new byte[32];

        using (
            RandomNumberGenerator rng =
            RandomNumberGenerator.Create()
        )
        {
            rng.GetBytes(bytes);
        }

        return Convert.ToBase64String(
            bytes
        );
    }

    // =========================================================
    // SAVE & EXIT
    // =========================================================

    public void SaveAndExit()
    {
        Save();

#if UNITY_EDITOR

        UnityEditor
            .EditorApplication
            .isPlaying = false;

#else

        Application.Quit();

#endif
    }

    // =========================================================
    // APPLICATION EVENTS
    // =========================================================

    private void OnApplicationPause(
        bool paused)
    {
        if (paused)
            Save();
    }

    private void OnApplicationQuit()
    {
        Time.timeScale = 1f;
        Save();
    }
}
