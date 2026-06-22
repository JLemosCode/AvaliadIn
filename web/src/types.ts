export interface ExperienceInput {
  title: string
  company: string
  description: string
}

export interface OpenToWorkInput {
  enabled: boolean
  recruitersOnly: boolean
  targetTitles: string[]
  remote: boolean
  contract: boolean
  fullTime: boolean
}

export interface ActivityInput {
  postsLast90Days: number
  commentsPerWeek: number
  invitesPerWeek: number
  profileViewsPerDay: number
  searchesPerWeek: number
  respondsToInMail: boolean
  creatorMode: boolean
}

export interface ProfileCompletenessInput {
  hasPhoto: boolean
  hasBanner: boolean
  hasFeatured: boolean
  recommendations: number
  endorsementsTopSkills: number
}

export interface ProfileEvaluationRequest {
  headline: string
  about: string
  experiences: ExperienceInput[]
  skills: string[]
  pinnedSkills: string[]
  openToWork: OpenToWorkInput
  activity: ActivityInput
  completeness: ProfileCompletenessInput
  targetRole: string
}

export interface CriterionScore {
  id: string
  label: string
  score: number
  maxScore: number
  feedback?: string
}

export interface PillarScore {
  id: string
  label: string
  score: number
  maxScore: number
  feedback?: string
}

export interface RdisResult {
  score: number
  level: string
  criteria: CriterionScore[]
}

export interface SsiJsResult {
  score: number
  level: string
  pillars: PillarScore[]
  partialScore: boolean
}

export interface CombinedResult {
  score: number
  level: string
}

export interface ProfileEvaluationResult {
  rdis: RdisResult
  ssiJs: SsiJsResult
  combined: CombinedResult
  topGaps: string[]
  weeklyActions: string[]
  warnings: string[]
  benchmark?: SsiBenchmark
}

export interface PillarBenchmark {
  id: string
  label: string
  yourScore: number
  networkAverage: number
  industryTop: number
}

export interface SsiBenchmark {
  yourScore: number
  networkAverage: number
  industryTop: number
  percentile: number
  segmentLabel: string
  comparisonSummary: string
  pillars: PillarBenchmark[]
  disclaimer: string
}

export interface AiAdvisorStatus {
  enabled: boolean
  provider: string
  model: string
  setupHint?: string
}

export interface ProfileAiInsights {
  available: boolean
  provider: string
  model: string
  summary: string
  headlineSuggestion?: string
  aboutSuggestion?: string
  prioritizedActions: string[]
  recruiterKeywords: string[]
  marketReadiness?: string
  recruiterSearchPreview?: string
  disclaimer?: string
}

export interface LinkedInImportResult {
  sourceUrl: string
  profile: ProfileEvaluationRequest
  quality: string
  warnings: string[]
  detectedFields: string[]
}

export type AppStep = 'home' | 'paste' | 'pdf' | 'manual' | 'loading' | 'report'

export interface LinkedInEvaluationResult {
  sourceUrl: string
  profile: ProfileEvaluationRequest
  evaluation: ProfileEvaluationResult
  quality: string
  importWarnings: string[]
  detectedFields: string[]
  aiInsights?: ProfileAiInsights | null
  aiLoading?: boolean
}
