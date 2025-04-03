using UnityEngine;

namespace TetrisGame
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        private int score = 0;
        private int planesCleared = 0;
        private int level = 1;

        // Bonus tracking
        private int consecutivePlanes = 0;
        private float lastClearTime = 0;
        private float comboTimeWindow = 3.0f; // seconds

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }

        public void AddScore(int planes)
        {
            // Base scores for clearing planes
            int[] scoreTable = { 0, 100, 300, 700, 1500 };
            
            // Calculate time bonus (faster clears = more points)
            float timeSinceLastClear = Time.time - lastClearTime;
            float timeBonus = 1.0f;
            
            if (timeSinceLastClear < comboTimeWindow)
            {
                consecutivePlanes++;
                timeBonus = Mathf.Max(1.0f, 2.0f - (timeSinceLastClear / comboTimeWindow));
            }
            else
            {
                consecutivePlanes = 1;
            }
            
            // Calculate combo multiplier (consecutive clears increase points)
            float comboMultiplier = 1.0f + (consecutivePlanes * 0.1f);
            
            // Award points - cap at 4+ for the table index
            int tableIndex = Mathf.Min(planes, 4);
            int baseScore = scoreTable[tableIndex];
            int bonusScore = Mathf.RoundToInt(baseScore * level * timeBonus * comboMultiplier);
            
            score += bonusScore;
            planesCleared += planes;
            lastClearTime = Time.time;

            // Level up logic
            if (planesCleared >= level * 5) // Made easier than 2D because planes are harder to clear
            {
                level++;
                GameManager.Instance.IncreaseSpeed();
            }
        }

        public int GetScore() 
        { 
            return score; 
        }
        
        public int GetLevel() 
        { 
            return level; 
        }
        
        public int GetLines() 
        { 
            return planesCleared; // Kept method name for UI compatibility
        }
    }
}
