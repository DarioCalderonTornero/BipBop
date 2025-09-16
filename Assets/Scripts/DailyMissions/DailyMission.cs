using System;

[System.Serializable]
public class DailyMission
{
    public MissionTemplate template;
    public int currentProgress;

    // Nuevo flag para controlar si ya se dio la recompensa
    public bool rewardClaimed;

    public bool IsCompleted => currentProgress >= template.goal;

    public DailyMission(MissionTemplate template)
    {
        this.template = template;
        this.currentProgress = 0;
        this.rewardClaimed = false; // al crear misión nunca está reclamada
    }

    public void AddProgress(int amount)
    {
        currentProgress += amount;
        if (currentProgress > template.goal)
            currentProgress = template.goal;
    }
}
