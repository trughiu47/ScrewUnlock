using UnityEngine;

[CreateAssetMenu(fileName = "Level_01", menuName = "ScrewUnlock/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int levelIndex = 1;
    public float timeLimit = 120f;

    [Header("Boards — thiet ke tung board tai day")]
    public BoardData[] boards = new BoardData[0];
}
