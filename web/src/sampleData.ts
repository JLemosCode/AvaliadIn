import type { ProfileEvaluationRequest } from './types'

export const sampleRequest: ProfileEvaluationRequest = {
  headline:
    'Consultor Sênior Full Stack | C# .NET Angular SQL | Liderança técnica & APIs REST | Seguros & Gov | 18+ anos',
  about:
    'Consultor Sênior Full Stack com 18+ anos em C#, .NET, Angular, SQL Server e APIs REST. Liderança técnica em seguros, governo e energia.\n\nStack: C# | .NET | ASP.NET | Angular | SQL Server | APIs REST | Git\n\nFormação: Análise e Desenvolvimento de Sistemas | MBA Gestão da Informação (em andamento)',
  experiences: [
    {
      title: 'Consultor Sênior Full Stack',
      company: 'JL LEMOS DIGITAL LTDA',
      description:
        '• Desenvolvimento full stack em C#, .NET, Angular e SQL Server.\n• Arquitetura de APIs REST e integrações.\n\nStack: C# | .NET | Angular | SQL Server',
    },
    {
      title: 'Desenvolvedor Full Stack Sênior',
      company: 'Ratto Software',
      description:
        '• Sistemas web em C#, ASP.NET MVC, Angular para Icatu Seguros.\n• Liderança técnica de equipe.\n\nStack: C# | ASP.NET MVC | Angular | SQL Server',
    },
  ],
  skills: [
    'C#',
    '.NET',
    'Angular',
    'ASP.NET MVC',
    'ASP.NET Core',
    'Full Stack Development',
    'APIs REST',
    'SQL Server',
    'JavaScript',
    'Liderança técnica',
    'Git',
  ],
  pinnedSkills: ['C#', '.NET', 'Angular'],
  openToWork: {
    enabled: true,
    recruitersOnly: true,
    targetTitles: [
      'Desenvolvedor Full Stack Sênior',
      'Consultor Full Stack',
      'Desenvolvedor .NET Sênior',
    ],
    remote: true,
    contract: true,
    fullTime: true,
  },
  activity: {
    postsLast90Days: 2,
    commentsPerWeek: 3,
    invitesPerWeek: 5,
    profileViewsPerDay: 2,
    searchesPerWeek: 5,
    respondsToInMail: true,
    creatorMode: false,
  },
  completeness: {
    hasPhoto: true,
    hasBanner: false,
    hasFeatured: true,
    recommendations: 2,
    endorsementsTopSkills: 3,
  },
  targetRole: 'Desenvolvedor Full Stack Sênior',
}

export function createEmptyRequest(): ProfileEvaluationRequest {
  return {
    headline: '',
    about: '',
    experiences: [{ title: '', company: '', description: '' }],
    skills: [],
    pinnedSkills: [],
    openToWork: {
      enabled: false,
      recruitersOnly: true,
      targetTitles: [],
      remote: false,
      contract: false,
      fullTime: true,
    },
    activity: {
      postsLast90Days: 0,
      commentsPerWeek: 0,
      invitesPerWeek: 0,
      profileViewsPerDay: 0,
      searchesPerWeek: 0,
      respondsToInMail: false,
      creatorMode: false,
    },
    completeness: {
      hasPhoto: false,
      hasBanner: false,
      hasFeatured: false,
      recommendations: 0,
      endorsementsTopSkills: 0,
    },
    targetRole: 'Desenvolvedor Full Stack Sênior',
  }
}

/** Garante objetos aninhados após import da API (evita null em openToWork.enabled etc.) */
export function normalizeProfileRequest(
  profile: Partial<ProfileEvaluationRequest>,
): ProfileEvaluationRequest {
  const defaults = createEmptyRequest()
  return {
    ...defaults,
    ...profile,
    experiences:
      profile.experiences && profile.experiences.length > 0
        ? profile.experiences
        : defaults.experiences,
    skills: profile.skills ?? defaults.skills,
    pinnedSkills: profile.pinnedSkills ?? defaults.pinnedSkills,
    openToWork: { ...defaults.openToWork, ...profile.openToWork },
    activity: { ...defaults.activity, ...profile.activity },
    completeness: { ...defaults.completeness, ...profile.completeness },
    targetRole: profile.targetRole?.trim() || profile.headline?.trim() || defaults.targetRole,
  }
}
