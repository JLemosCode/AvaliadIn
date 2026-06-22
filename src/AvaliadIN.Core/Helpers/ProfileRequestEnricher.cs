using AvaliadIN.Core.Models;

namespace AvaliadIN.Core.Helpers;

public static class ProfileRequestEnricher
{
    public static ProfileEvaluationRequest Enrich(ProfileEvaluationRequest profile)
    {
        return profile with
        {
            OpenToWork = profile.OpenToWork ?? new OpenToWorkInput(),
            Activity = profile.Activity ?? new ActivityInput(),
            Completeness = profile.Completeness ?? new ProfileCompletenessInput
            {
                HasPhoto = !string.IsNullOrWhiteSpace(profile.Headline)
            },
            Experiences = profile.Experiences.Count > 0
                ? profile.Experiences
                : [new ExperienceInput()],
            PinnedSkills = profile.PinnedSkills.Count > 0
                ? profile.PinnedSkills
                : profile.Skills.Take(3).ToList(),
            TargetRole = string.IsNullOrWhiteSpace(profile.TargetRole)
                ? profile.Headline
                : profile.TargetRole
        };
    }

    public static bool HasMinimumContent(ProfileEvaluationRequest profile)
    {
        var headline = LinkedInAuthWallDetector.SanitizeHeadline(profile.Headline);
        var about = LinkedInAuthWallDetector.IsBlockedHeadline(profile.About) ? null : profile.About;
        return !string.IsNullOrWhiteSpace(headline) || !string.IsNullOrWhiteSpace(about);
    }
}
