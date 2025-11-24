using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "NewMissionTemplate", menuName = "DailyMissions/MissionTemplate")]
public class MissionTemplate : ScriptableObject
{
    public string id;

    public LocalizedString description;
    public int goal = 1;

    public int coinReward = 10;
    public int xpReward = 10;

    public Sprite xpIcon;
    public Sprite coinIcon;
}
