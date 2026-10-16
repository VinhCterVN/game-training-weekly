using System.Collections.Generic;
using UnityEngine;

namespace TuberLevels
{
    [CreateAssetMenu(menuName = "Tube Levels/Level Database")]
    public sealed class LevelDatabaseSO : ScriptableObject
    {
        public List<LevelDataSO> levels = new List<LevelDataSO>();
    }
}