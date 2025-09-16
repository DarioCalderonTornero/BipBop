using System;

[System.Serializable]
public class DailyMission
{
    public MissionTemplate template;
    public int currentProgress;
    public bool IsCompleted => currentProgress >= template.goal;

    public DailyMission(MissionTemplate template)
    {
        this.template = template;
        this.currentProgress = 0;
    }

    public void AddProgress(int amount)
    {
        currentProgress += amount;
        if (currentProgress > template.goal)
            currentProgress = template.goal;
    }
}
