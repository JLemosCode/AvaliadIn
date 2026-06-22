import type {
  AiAdvisorStatus,
  LinkedInEvaluationResult,
  LinkedInImportResult,
  ProfileAiInsights,
  ProfileEvaluationRequest,
  ProfileEvaluationResult,
} from './types'
import { normalizeProfileRequest } from './sampleData'

/** Base da API — usa proxy nginx (mesma origem) quando VITE_API_URL não está definido */
export function getApiBase(): string {
  const env = import.meta.env.VITE_API_URL
  if (env) return env.replace(/\/$/, '')
  if (typeof window !== 'undefined') return window.location.origin
  return ''
}

export interface LinkedInSessionInfo {
  connected: boolean
  connectedAtUtc: string | null
  loginInProgress: boolean
  method: string | null
  interactiveLoginAvailable: boolean
}

export async function getLinkedInSession(): Promise<LinkedInSessionInfo> {
  const response = await fetch(`${getApiBase()}/api/v1/linkedin/session`)
  if (!response.ok) throw new Error('Não foi possível verificar a sessão LinkedIn.')
  return response.json()
}

export async function connectLinkedInCookie(liAt: string): Promise<void> {
  const response = await fetch(`${getApiBase()}/api/v1/linkedin/session/cookie`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ liAt }),
  })

  if (!response.ok) {
    const body = await response.json().catch(() => null)
    throw new Error(
      body && typeof body.error === 'string' ? body.error : 'Falha ao conectar LinkedIn.',
    )
  }
}

export async function startLinkedInInteractiveLogin(): Promise<void> {
  const response = await fetch(`${getApiBase()}/api/v1/linkedin/session/interactive`, {
    method: 'POST',
  })

  if (!response.ok) {
    const body = await response.json().catch(() => null)
    throw new Error(
      body && typeof body.error === 'string'
        ? body.error
        : 'Login interativo indisponível neste ambiente.',
    )
  }
}

export async function disconnectLinkedIn(): Promise<void> {
  await fetch(`${getApiBase()}/api/v1/linkedin/session`, { method: 'DELETE' })
}

export async function evaluateLinkedInUrl(url: string): Promise<LinkedInEvaluationResult> {
  const response = await fetch(`${getApiBase()}/api/v1/evaluate-url`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ url }),
  })

  if (!response.ok) {
    const body = await response.json().catch(() => null)
    const message =
      body && typeof body.error === 'string'
        ? body.error
        : body && typeof body.detail === 'string'
          ? body.detail
          : `Erro ${response.status}: falha na captura`
    throw new Error(message)
  }

  const data: LinkedInEvaluationResult = await response.json()
  return {
    ...data,
    profile: normalizeProfileRequest(data.profile),
  }
}

export async function importLinkedInPdf(file: File): Promise<LinkedInImportResult> {
  const form = new FormData()
  form.append('file', file)

  const response = await fetch(`${getApiBase()}/api/v1/import/pdf`, {
    method: 'POST',
    body: form,
  })

  if (!response.ok) {
    const body = await response.json().catch(() => null)
    const message =
      body && typeof body.error === 'string'
        ? body.error
        : `Erro ${response.status}: falha ao ler PDF`
    throw new Error(message)
  }

  const data: LinkedInImportResult = await response.json()
  return {
    ...data,
    profile: normalizeProfileRequest(data.profile),
  }
}

export async function evaluateCapturedProfile(
  profile: ProfileEvaluationRequest,
  sourceUrl: string,
  options?: { withAi?: boolean; onPhase?: (phase: 'evaluate' | 'ai') => void },
): Promise<LinkedInEvaluationResult> {
  const normalized = normalizeProfileRequest(profile)

  if (!normalized.headline.trim() && !normalized.about.trim()) {
    throw new Error('Perfil sem headline ou sobre. Abra a página completa do LinkedIn.')
  }

  options?.onPhase?.('evaluate')

  const response = await fetch(`${getApiBase()}/api/v1/evaluate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(normalized),
  })

  if (!response.ok) {
    const body = await response.json().catch(() => null)
    const message =
      body && typeof body.error === 'string'
        ? body.error
        : `Erro ${response.status}: falha na avaliação`
    throw new Error(message)
  }

  const evaluation: ProfileEvaluationResult = await response.json()
  const detected: string[] = []
  if (normalized.headline) detected.push('headline')
  if (normalized.about) detected.push('about')
  if (normalized.experiences.some((e) => e.title.trim())) detected.push('experiences')
  if (normalized.skills.length) detected.push('skills')

  const base: LinkedInEvaluationResult = {
    sourceUrl,
    profile: normalized,
    evaluation,
    quality: detected.length >= 3 ? 'good' : 'partial',
    importWarnings: [],
    detectedFields: detected,
    aiInsights: null,
    aiLoading: false,
  }

  const withAi = options?.withAi !== false
  if (!withAi) return base

  let aiEnabled = false
  try {
    const status = await getAiStatus()
    aiEnabled = status.enabled
  } catch {
    return base
  }

  if (!aiEnabled) return base

  options?.onPhase?.('ai')

  try {
    const aiInsights = await fetchAiInsights(normalized, evaluation)
    return { ...base, aiInsights, aiLoading: false }
  } catch (err) {
    return {
      ...base,
      aiInsights: {
        available: false,
        provider: '',
        model: '',
        summary: '',
        prioritizedActions: [],
        recruiterKeywords: [],
        disclaimer:
          err instanceof Error ? err.message : 'Validação com IA indisponível neste momento.',
      },
      aiLoading: false,
    }
  }
}

export async function getAiStatus(): Promise<AiAdvisorStatus> {
  const response = await fetch(`${getApiBase()}/api/v1/ai/status`)
  if (!response.ok) throw new Error('Não foi possível verificar status da IA.')
  return response.json()
}

export async function fetchAiInsights(
  profile: ProfileEvaluationRequest,
  evaluation: ProfileEvaluationResult,
): Promise<ProfileAiInsights> {
  const response = await fetch(`${getApiBase()}/api/v1/evaluate/ai-insights`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ profile, evaluation }),
  })

  if (!response.ok) {
    const body = await response.json().catch(() => null)
    const message =
      body && typeof body.error === 'string'
        ? body.error
        : `Erro ${response.status}: falha ao gerar insights`
    throw new Error(message)
  }

  return response.json()
}
