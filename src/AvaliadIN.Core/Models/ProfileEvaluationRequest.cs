namespace AvaliadIN.Core.Models;

public sealed record ProfileEvaluationRequest
{
    public string Headline { get; init; } = string.Empty;
    public string About { get; init; } = string.Empty;
    public IReadOnlyList<ExperienceInput> Experiences { get; init; } = [];
    public IReadOnlyList<string> Skills { get; init; } = [];
    public IReadOnlyList<string> PinnedSkills { get; init; } = [];
    public OpenToWorkInput? OpenToWork { get; init; }
    public ActivityInput? Activity { get; init; }
    public ProfileCompletenessInput? Completeness { get; init; }
    public string TargetRole { get; init; } = "Desenvolvedor Full Stack Sênior";
}

public sealed record ExperienceInput
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
}

public sealed record OpenToWorkInput
{
    public bool Enabled { get; init; }
    public bool RecruitersOnly { get; init; }
    public IReadOnlyList<string> TargetTitles { get; init; } = [];
    public bool Remote { get; init; }
    public bool Contract { get; init; }
    public bool FullTime { get; init; }
}

public sealed record ActivityInput
{
    public int PostsLast90Days { get; init; }
    public int CommentsPerWeek { get; init; }
    public int InvitesPerWeek { get; init; }
    public int ProfileViewsPerDay { get; init; }
    public int SearchesPerWeek { get; init; }
    public bool RespondsToInMail { get; init; }
    public bool CreatorMode { get; init; }
}

public sealed record ProfileCompletenessInput
{
    public bool HasPhoto { get; init; }
    public bool HasBanner { get; init; }
    public bool HasFeatured { get; init; }
    public int Recommendations { get; init; }
    public int EndorsementsTopSkills { get; init; }
}
