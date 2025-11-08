using UnityEngine;

public static class LevelProgress
{
    const string MaxUnlockedKey = "MaxUnlockedLevelIndex";

    public static int GetMaxUnlocked()
    {
        return PlayerPrefs.GetInt(MaxUnlockedKey, 0); // 0 = solo primer nivel (índice en Build)
    }

    public static void UnlockUpTo(int buildIndex)
    {
        int current = GetMaxUnlocked();
        if (buildIndex > current)
        {
            PlayerPrefs.SetInt(MaxUnlockedKey, buildIndex);
            PlayerPrefs.Save();
        }
    }

    public static bool IsUnlocked(int buildIndex)
    {
        return buildIndex <= GetMaxUnlocked();
    }
}
