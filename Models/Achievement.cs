using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ComicReader.Models
{
    /// <summary>
    /// Categorías de logros
    /// </summary>
    public enum AchievementCategory
    {
        Reading,        // Relacionados con lectura
        Collection,     // Relacionados con colecciones
        Organization,   // Relacionados con organización
        Exploration,    // Relacionados con exploración
        Social,         // Relacionados con compartir
        Time            // Relacionados con tiempo de uso
    }

    /// <summary>
    /// Representa un logro del usuario
    /// </summary>
    public class Achievement
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AchievementCategory Category { get; set; }
        public string IconEmoji { get; set; } = "🏆";
        public int Points { get; set; } = 10;
        public bool IsUnlocked { get; set; } = false;
        public DateTime? UnlockedDate { get; set; }
        public double Progress { get; set; } = 0; // 0.0 a 1.0
        public int CurrentValue { get; set; } = 0;
        public int TargetValue { get; set; } = 1;
        public bool IsSecret { get; set; } = false; // Logro secreto (no se muestra descripción hasta desbloquear)
    }

    /// <summary>
    /// Gestiona el sistema de logros
    /// </summary>
    public class AchievementManager
    {
        private readonly string _achievementsFile;
        private List<Achievement> _achievements = new List<Achievement>();
        private Dictionary<string, int> _userStats = new Dictionary<string, int>();

        public event EventHandler<Achievement>? AchievementUnlocked;

        public AchievementManager(string dataFolder = "")
        {
            if (string.IsNullOrEmpty(dataFolder))
            {
                dataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PercysLibrary"
                );
            }
            Directory.CreateDirectory(dataFolder);
            _achievementsFile = Path.Combine(dataFolder, "achievements.json");
            InitializeAchievements();
            LoadProgress();
        }

        private void InitializeAchievements()
        {
            _achievements = new List<Achievement>
            {
                // Logros de lectura
                new Achievement { Id = "first_comic", Name = "Primer Paso", Description = "Lee tu primer cómic", Category = AchievementCategory.Reading, IconEmoji = "📖", Points = 10, TargetValue = 1 },
                new Achievement { Id = "read_10", Name = "Lector Casual", Description = "Lee 10 cómics diferentes", Category = AchievementCategory.Reading, IconEmoji = "📚", Points = 25, TargetValue = 10 },
                new Achievement { Id = "read_50", Name = "Lector Dedicado", Description = "Lee 50 cómics diferentes", Category = AchievementCategory.Reading, IconEmoji = "📖", Points = 50, TargetValue = 50 },
                new Achievement { Id = "read_100", Name = "Biblioteca Viviente", Description = "Lee 100 cómics diferentes", Category = AchievementCategory.Reading, IconEmoji = "🎓", Points = 100, TargetValue = 100 },
                new Achievement { Id = "read_1000_pages", Name = "Devorador de Páginas", Description = "Lee 1000 páginas en total", Category = AchievementCategory.Reading, IconEmoji = "⚡", Points = 75, TargetValue = 1000 },
                new Achievement { Id = "night_owl", Name = "Búho Nocturno", Description = "Lee después de medianoche", Category = AchievementCategory.Reading, IconEmoji = "🦉", Points = 15, TargetValue = 1 },
                new Achievement { Id = "early_bird", Name = "Madrugador", Description = "Lee antes de las 6 AM", Category = AchievementCategory.Reading, IconEmoji = "🐦", Points = 15, TargetValue = 1 },
                new Achievement { Id = "marathon", Name = "Maratón", Description = "Lee durante 3 horas seguidas", Category = AchievementCategory.Reading, IconEmoji = "🏃", Points = 30, TargetValue = 1 },
                new Achievement { Id = "speed_reader", Name = "Lector Veloz", Description = "Lee 100 páginas en una hora", Category = AchievementCategory.Reading, IconEmoji = "💨", Points = 40, TargetValue = 1 },
                
                // Logros de colección
                new Achievement { Id = "first_collection", Name = "Coleccionista Novato", Description = "Crea tu primera colección", Category = AchievementCategory.Collection, IconEmoji = "📦", Points = 10, TargetValue = 1 },
                new Achievement { Id = "collection_10", Name = "Organizador", Description = "Crea 10 colecciones", Category = AchievementCategory.Collection, IconEmoji = "🗂️", Points = 30, TargetValue = 10 },
                new Achievement { Id = "big_collection", Name = "Mega Colección", Description = "Ten una colección con 50+ cómics", Category = AchievementCategory.Collection, IconEmoji = "📚", Points = 50, TargetValue = 1 },
                
                // Logros de organización
                new Achievement { Id = "first_bookmark", Name = "Marcador Principiante", Description = "Crea tu primer marcador", Category = AchievementCategory.Organization, IconEmoji = "🔖", Points = 5, TargetValue = 1 },
                new Achievement { Id = "bookmark_master", Name = "Maestro de Marcadores", Description = "Crea 50 marcadores", Category = AchievementCategory.Organization, IconEmoji = "📌", Points = 35, TargetValue = 50 },
                new Achievement { Id = "first_annotation", Name = "Anotador", Description = "Crea tu primera anotación", Category = AchievementCategory.Organization, IconEmoji = "✏️", Points = 10, TargetValue = 1 },
                new Achievement { Id = "annotator_pro", Name = "Anotador Pro", Description = "Crea 100 anotaciones", Category = AchievementCategory.Organization, IconEmoji = "📝", Points = 50, TargetValue = 100 },
                
                // Logros de exploración
                new Achievement { Id = "format_explorer", Name = "Explorador de Formatos", Description = "Lee cómics en 5 formatos diferentes", Category = AchievementCategory.Exploration, IconEmoji = "🔍", Points = 20, TargetValue = 5 },
                new Achievement { Id = "theme_switcher", Name = "Camaleón", Description = "Prueba 10 temas diferentes", Category = AchievementCategory.Exploration, IconEmoji = "🎨", Points = 15, TargetValue = 10 },
                new Achievement { Id = "settings_master", Name = "Maestro de Configuración", Description = "Personaliza 20 configuraciones", Category = AchievementCategory.Exploration, IconEmoji = "⚙️", Points = 25, TargetValue = 20 },
                
                // Logros de tiempo
                new Achievement { Id = "week_streak", Name = "Semana Consecutiva", Description = "Lee al menos una página cada día durante una semana", Category = AchievementCategory.Time, IconEmoji = "🔥", Points = 30, TargetValue = 7 },
                new Achievement { Id = "month_streak", Name = "Mes Consecutivo", Description = "Lee al menos una página cada día durante un mes", Category = AchievementCategory.Time, IconEmoji = "💪", Points = 100, TargetValue = 30 },
                new Achievement { Id = "veteran", Name = "Veterano", Description = "Usa la aplicación durante 365 días", Category = AchievementCategory.Time, IconEmoji = "👑", Points = 200, TargetValue = 365 },
                
                // Logros secretos
                new Achievement { Id = "secret_midnight", Name = "???", Description = "Lee exactamente a las 12:00 AM", Category = AchievementCategory.Reading, IconEmoji = "🌙", Points = 50, TargetValue = 1, IsSecret = true },
                new Achievement { Id = "secret_page_666", Name = "???", Description = "Llega a la página 666 de un cómic", Category = AchievementCategory.Reading, IconEmoji = "😈", Points = 66, TargetValue = 1, IsSecret = true },
                new Achievement { Id = "secret_perfect_score", Name = "???", Description = "Completa un cómic con 100% de las páginas leídas", Category = AchievementCategory.Reading, IconEmoji = "💯", Points = 75, TargetValue = 1, IsSecret = true }
            };
        }

        private void LoadProgress()
        {
            try
            {
                if (File.Exists(_achievementsFile))
                {
                    var json = File.ReadAllText(_achievementsFile);
                    var data = System.Text.Json.JsonSerializer.Deserialize<AchievementSaveData>(json);
                    if (data != null)
                    {
                        _userStats = data.UserStats ?? new Dictionary<string, int>();
                        UpdateAchievementsFromStats(data.UnlockedAchievements ?? new List<string>());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading achievements: {ex.Message}");
            }
        }

        private void UpdateAchievementsFromStats(List<string> unlockedIds)
        {
            foreach (var achievement in _achievements)
            {
                if (unlockedIds.Contains(achievement.Id))
                {
                    achievement.IsUnlocked = true;
                }

                if (_userStats.TryGetValue(achievement.Id, out int value))
                {
                    achievement.CurrentValue = value;
                    achievement.Progress = Math.Min(1.0, (double)value / achievement.TargetValue);
                }
            }
        }

        public void SaveProgress()
        {
            try
            {
                var data = new AchievementSaveData
                {
                    UnlockedAchievements = _achievements.Where(a => a.IsUnlocked).Select(a => a.Id).ToList(),
                    UserStats = _userStats
                };

                var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_achievementsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving achievements: {ex.Message}");
            }
        }

        public void IncrementStat(string statId, int amount = 1)
        {
            if (!_userStats.ContainsKey(statId))
            {
                _userStats[statId] = 0;
            }
            _userStats[statId] += amount;

            CheckAchievements(statId);
            SaveProgress();
        }

        private void CheckAchievements(string statId)
        {
            var relatedAchievements = _achievements.Where(a => a.Id == statId && !a.IsUnlocked);
            
            foreach (var achievement in relatedAchievements)
            {
                if (_userStats.TryGetValue(statId, out int value))
                {
                    achievement.CurrentValue = value;
                    achievement.Progress = Math.Min(1.0, (double)value / achievement.TargetValue);

                    if (value >= achievement.TargetValue)
                    {
                        UnlockAchievement(achievement);
                    }
                }
            }
        }

        private void UnlockAchievement(Achievement achievement)
        {
            if (achievement.IsUnlocked) return;

            achievement.IsUnlocked = true;
            achievement.UnlockedDate = DateTime.Now;
            achievement.Progress = 1.0;

            AchievementUnlocked?.Invoke(this, achievement);
            SaveProgress();
        }

        public List<Achievement> GetAllAchievements()
        {
            return _achievements.ToList();
        }

        public List<Achievement> GetUnlockedAchievements()
        {
            return _achievements.Where(a => a.IsUnlocked).ToList();
        }

        public List<Achievement> GetLockedAchievements()
        {
            return _achievements.Where(a => !a.IsUnlocked).ToList();
        }

        public int GetTotalPoints()
        {
            return _achievements.Where(a => a.IsUnlocked).Sum(a => a.Points);
        }

        public double GetCompletionPercentage()
        {
            if (_achievements.Count == 0) return 0;
            return (double)_achievements.Count(a => a.IsUnlocked) / _achievements.Count * 100;
        }
    }

    // Clase auxiliar para serialización
    public class AchievementSaveData
    {
        public List<string> UnlockedAchievements { get; set; } = new List<string>();
        public Dictionary<string, int> UserStats { get; set; } = new Dictionary<string, int>();
    }
}
