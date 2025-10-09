using UnityEngine;

[CreateAssetMenu(fileName = "NewMissionTemplate", menuName = "DailyMissions/MissionTemplate")]
public class MissionTemplate : ScriptableObject
{
    public string id;                 // Identificador único de la misión
    public string description;        // Texto de la misión (ej: "Juega 3 partidas al modo Geométrico")
    public int goal = 1;              // Objetivo de la misión (ej: 3 partidas)
    public int reward = 10;           // Recompensa (ej: monedas)
    public int xpReward = 10;
    public Sprite icon;               // Icono que se muestra en UI
}
