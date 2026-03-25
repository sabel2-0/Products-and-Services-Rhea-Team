namespace MyAspNetApp.Models
{
    public class ChallengesPageViewModel
    {
        public string SeasonLabel { get; set; } = string.Empty;
        public int ActiveCount { get; set; }
        public int TotalParticipants { get; set; }
        public decimal TotalGoalKm { get; set; }
        public int DaysUntilNextDrop { get; set; }
        public ChallengeCardViewModel? FeaturedChallenge { get; set; }
        public List<ChallengeCardViewModel> UpcomingChallenges { get; set; } = new();
        public List<ChallengeCardViewModel> TopChallenges { get; set; } = new();
    }

    public class ChallengeCardViewModel
    {
        public int ChallengeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Rules { get; set; } = string.Empty;
        public string Prizes { get; set; } = string.Empty;
        public decimal GoalKm { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalParticipants { get; set; }
        public int TotalCompleted { get; set; }
        public double CompletionPercent { get; set; }
        public int DurationDays { get; set; }
        public string DifficultyLabel { get; set; } = string.Empty;
        public string DifficultyCssClass { get; set; } = string.Empty;
    }
}
