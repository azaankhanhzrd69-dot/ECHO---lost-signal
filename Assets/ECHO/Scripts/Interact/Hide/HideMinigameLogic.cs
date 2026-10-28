using System;

namespace Echo.Interact
{
    /// <summary>
    /// Pure mathematical logic for the hide reticle and banging meter.
    /// Free of UnityEngine types for clean unit testing.
    /// </summary>
    public static class HideMinigameLogic
    {
        /// <summary>
        /// Checks if reticle offset from center is within the ring radius.
        /// </summary>
        public static bool IsInsideRing(float x, float y, float ringRadius)
        {
            return (x * x + y * y) <= (ringRadius * ringRadius);
        }

        /// <summary>
        /// Updates the banging meter based on whether reticle is inside or outside the ring.
        /// Fills when outside, decays when inside. Clamped between 0.0 and 1.0.
        /// </summary>
        public static float UpdateMeter(float currentMeter, bool isInsideRing, float fillRate, float decayRate, float deltaTime)
        {
            if (isInsideRing)
            {
                currentMeter -= decayRate * deltaTime;
            }
            else
            {
                currentMeter += fillRate * deltaTime;
            }

            if (currentMeter < 0f) return 0f;
            if (currentMeter > 1f) return 1f;
            return currentMeter;
        }

        /// <summary>
        /// Evaluates current minigame progress.
        /// Returns -1 for failed (meter filled), 1 for survived (timer elapsed), or 0 for still running.
        /// </summary>
        public static int Evaluate(float meter, float elapsedTime, float totalDuration)
        {
            if (meter >= 1.0f)
                return -1; // Failed

            if (elapsedTime >= totalDuration)
                return 1; // Succeeded

            return 0; // In progress
        }
    }
}

