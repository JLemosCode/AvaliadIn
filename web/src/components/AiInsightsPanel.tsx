import { useEffect, useState } from 'react'
import { fetchAiInsights, getAiStatus } from '../api'
import type {
  AiAdvisorStatus,
  ProfileAiInsights,
  ProfileEvaluationRequest,
  ProfileEvaluationResult,
} from '../types'

interface AiInsightsPanelProps {
  profile: ProfileEvaluationRequest
  evaluation: ProfileEvaluationResult
  initialInsights?: ProfileAiInsights | null
  autoLoaded?: boolean
}

export function AiInsightsPanel({
  profile,
  evaluation,
  initialInsights,
  autoLoaded = false,
}: AiInsightsPanelProps) {
  const [status, setStatus] = useState<AiAdvisorStatus | null>(null)
  const [insights, setInsights] = useState<ProfileAiInsights | null>(initialInsights ?? null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(
    initialInsights && !initialInsights.available ? initialInsights.disclaimer ?? null : null,
  )

  useEffect(() => {
    setInsights(initialInsights ?? null)
    if (initialInsights && !initialInsights.available && initialInsights.disclaimer) {
      setError(initialInsights.disclaimer)
    }
  }, [initialInsights])

  useEffect(() => {
    getAiStatus()
      .then(setStatus)
      .catch(() =>
        setStatus({
          enabled: false,
          provider: 'openai-compatible',
          model: '',
          setupHint: 'Não foi possível verificar o status da IA.',
        }),
      )
  }, [])

  const handleGenerate = async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await fetchAiInsights(profile, evaluation)
      setInsights(result)
      if (!result.available && result.disclaimer) setError(result.disclaimer)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao gerar insights')
    } finally {
      setLoading(false)
    }
  }

  const showRetry = !loading && (!insights?.available || !autoLoaded)

  return (
    <section className="card card--ai">
      <div className="card__row">
        <div>
          <h3>Validação de mercado com IA</h3>
          <p className="card__subtitle">
            {autoLoaded && insights?.available
              ? 'Validação automática após a avaliação — simula buscas de recrutadores e IA de hiring.'
              : 'Complementa RDIS e SSI-JS com coaching alinhado ao cargo alvo.'}
          </p>
        </div>
        {status?.enabled && (
          <span className="ai-badge ai-badge--on">IA ativa · {status.model}</span>
        )}
      </div>

      {!status?.enabled && status?.setupHint && !insights && (
        <div className="alert alert--info">
          <strong>IA desligada</strong>
          <p>{status.setupHint}</p>
        </div>
      )}

      {showRetry && (
        <button
          type="button"
          className="btn btn--primary"
          onClick={() => void handleGenerate()}
          disabled={loading}
        >
          {loading ? 'Validando...' : insights ? 'Validar novamente com IA' : 'Validar currículo com IA'}
        </button>
      )}

      {error && !insights?.available && <div className="alert alert--error">{error}</div>}

      {insights?.available && (
        <div className="ai-insights">
          <div className="ai-insights__meta">
            {insights.marketReadiness && (
              <span className={`ai-readiness ai-readiness--${readinessTone(insights.marketReadiness)}`}>
                Mercado: {insights.marketReadiness}
              </span>
            )}
            {autoLoaded && <span className="ai-auto-badge">Gerado automaticamente</span>}
          </div>
          <p className="ai-insights__summary">{insights.summary}</p>

          {insights.recruiterSearchPreview && (
            <div className="ai-insights__block ai-insights__block--search">
              <strong>Como você aparece numa busca de recrutador</strong>
              <p>{insights.recruiterSearchPreview}</p>
            </div>
          )}

          {insights.headlineSuggestion && (
            <div className="ai-insights__block">
              <strong>Headline sugerida</strong>
              <p>{insights.headlineSuggestion}</p>
            </div>
          )}

          {insights.aboutSuggestion && (
            <div className="ai-insights__block">
              <strong>Sobre — sugestão de abertura</strong>
              <p>{insights.aboutSuggestion}</p>
            </div>
          )}

          {insights.recruiterKeywords.length > 0 && (
            <div className="ai-insights__block">
              <strong>Keywords para recrutadores</strong>
              <div className="ai-keywords">
                {insights.recruiterKeywords.map((kw) => (
                  <span key={kw} className="ai-keyword">
                    {kw}
                  </span>
                ))}
              </div>
            </div>
          )}

          {insights.prioritizedActions.length > 0 && (
            <div className="ai-insights__block">
              <strong>Ações prioritárias (IA)</strong>
              <ol className="action-list">
                {insights.prioritizedActions.map((action) => (
                  <li key={action}>{action}</li>
                ))}
              </ol>
            </div>
          )}

          {insights.disclaimer && (
            <p className="ai-insights__disclaimer">{insights.disclaimer}</p>
          )}
        </div>
      )}
    </section>
  )
}

function readinessTone(level: string): string {
  const n = level.toLowerCase()
  if (n.includes('alto')) return 'high'
  if (n.includes('baixo')) return 'low'
  return 'mid'
}
