using UnityEngine;
using UnityEngine.AI;

namespace SurvivalEngine
{
    public static class AIMovementReachability
    {
        private const float WaterProbeHeight = 1.5f;
        private const float WaterProbeDistance = 4f;
        private const float DefaultSampleSpacing = 1f;

        public static bool HasReachablePath(PlayerCharacter player, Vector3 fromPosition, Vector3 toPosition)
        {
            if (!TryCalculatePath(fromPosition, toPosition, out var path))
                return false;

            return !PathCrossesWater(player, path, DefaultSampleSpacing);
        }

        private static bool TryCalculatePath(Vector3 fromPosition, Vector3 toPosition, out NavMeshPath path)
        {
            path = new NavMeshPath();
            bool success = NavMesh.CalculatePath(fromPosition, toPosition, NavMesh.AllAreas, path);
            return success && path.status == NavMeshPathStatus.PathComplete && path.corners != null && path.corners.Length > 0;
        }

        private static bool PathCrossesWater(PlayerCharacter player, NavMeshPath path, float sampleSpacing)
        {
            var swim = player != null ? player.GetComponent<PlayerCharacterSwim>() : null;
            int waterMask = swim != null ? swim.water_layer.value : 0;
            if (waterMask == 0)
                return false;

            var corners = path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Vector3 start = corners[i];
                Vector3 end = corners[i + 1];
                float distance = Vector3.Distance(start, end);
                int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.25f, sampleSpacing)));
                for (int step = 0; step <= steps; step++)
                {
                    float t = steps == 0 ? 0f : step / (float)steps;
                    Vector3 point = Vector3.Lerp(start, end, t);
                    if (IsWaterAt(point, waterMask))
                        return true;
                }
            }

            return false;
        }

        private static bool IsWaterAt(Vector3 point, int waterMask)
        {
            Vector3 origin = point + Vector3.up * WaterProbeHeight;
            return Physics.Raycast(origin, Vector3.down, WaterProbeDistance, waterMask, QueryTriggerInteraction.Collide);
        }
    }
}
