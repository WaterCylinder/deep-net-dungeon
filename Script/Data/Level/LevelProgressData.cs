using Godot;
using Godot.Collections;

/// <summary>
/// 关卡进度数据（临时运行时）
/// </summary>
public class LevelProgressData
{
    public string characterId = "";
    public int currentLevel = 1;
    public int score = 0;
    public float playTime = 0f;
    public int enemiesKilled = 0;
    public int itemsCollected = 0;
    public Dictionary<string, int> milestones = new();

    public LevelProgressData()
    {
    }

    public LevelProgressData(string charId)
    {
        characterId = charId;
    }
}
