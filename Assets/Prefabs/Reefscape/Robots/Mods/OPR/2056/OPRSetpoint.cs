using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.OPR._2056
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/OPR Setpoint", order = 0)]
    public class OPRSetpoint : ScriptableObject
    {
        [Tooltip("Deg")] public float armAngle;
        [Tooltip("Inch")] public float elevatorHeight;
        [Tooltip("Deg")] public float intakeAngle;
        [Tooltip("Deg")] public float climberAngle;
        [Tooltip("Deg")] public float noWrapAngle;
    }
}