using System;
using System.Collections.Generic;

namespace ComicReader.Core.Abstractions
{
    public interface IReadingStatsService
    {
        void StartSession(string comicPath, string comicTitle, int totalPages);
        void EndSession();
        void RecordPageViewed(int pageNumber);

        StatsDashboard GetDashboard();
        IEnumerable<ComicProgressInfo> GetRecentProgress(int count = 10);
        IEnumerable<ReadingSessionInfo> GetTodaySessions();

        void ResetAll();
        void ExportSessionsToCsv(string filePath);
    }

    public class StatsDashboard
    {
        public int TotalComicsRead { get; set; }
        public int TotalPagesRead { get; set; }
        public TimeSpan TotalReadingTime { get; set; }
        public TimeSpan AverageReadingTime { get; set; }
        public int ComicsThisWeek { get; set; }
        public int ComicsThisMonth { get; set; }
        public string FavoriteGenre { get; set; } = "Desconocido"; // No se calcula por ahora
        public string FavoriteDay { get; set; } = "—";
        public string PreferredFormat { get; set; } = "—";
        public TimeSpan LongestReadingSession { get; set; }
        public int CurrentStreak { get; set; }
    }

    public class ComicProgressInfo
    {
        public string Title { get; set; }
        public string ComicPath { get; set; }
        public int Progress { get; set; }
        public int TotalPages { get; set; }
        public DateTime LastRead { get; set; }
    }

    public class ReadingSessionInfo
    {
        public string ComicTitle { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int PagesRead { get; set; }
    }
}
