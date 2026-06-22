import type { ProfileEvaluationResult } from '../types'
import { ScoreGauge } from './ScoreGauge'

interface ResultsPanelProps {
  result: ProfileEvaluationResult
}

function ScoreBar({ label, score, maxScore, feedback }: {
  label: string
  score: number
  maxScore: number
  feedback?: string
}) {
  const pct = maxScore > 0 ? Math.round((score / maxScore) * 100) : 0
  return (
    <div className="score-bar">
      <div className="score-bar__header">
        <span>{label}</span>
        <span className="score-bar__points">{score}/{maxScore}</span>
      </div>
      <div className="score-bar__track">
        <div className="score-bar__fill" style={{ width: `${pct}%` }} />
      </div>
      {feedback && <p className="score-bar__feedback">{feedback}</p>}
    </div>
  )
}

export function ResultsPanel({ result }: ResultsPanelProps) {
  return (
    <div className="results">
      <header className="results__header">
        <h2>Resultado da avaliação</h2>
        <p>Scores baseados em RDIS (recrutadores) e SSI-JS (atividade social).</p>
      </header>

      <div className="results__gauges">
        <ScoreGauge
          label="RDIS"
          score={result.rdis.score}
          level={result.rdis.level}
          accent="primary"
        />
        <ScoreGauge
          label="SSI-JS"
          score={result.ssiJs.score}
          level={result.ssiJs.level}
          accent="secondary"
        />
        <ScoreGauge
          label="Combinado"
          score={result.combined.score}
          level={result.combined.level}
          accent="combined"
        />
      </div>

      {result.ssiJs.partialScore && (
        <div className="alert alert--warning">
          Score SSI-JS parcial — preencha mais dados de atividade para um resultado completo.
        </div>
      )}

      {result.warnings.length > 0 && (
        <div className="alert alert--warning">
          <strong>Avisos</strong>
          <ul>
            {result.warnings.map((w) => (
              <li key={w}>{w}</li>
            ))}
          </ul>
        </div>
      )}

      <section className="card">
        <h3>Critérios RDIS</h3>
        {result.rdis.criteria.map((c) => (
          <ScoreBar
            key={c.id}
            label={c.label}
            score={c.score}
            maxScore={c.maxScore}
            feedback={c.feedback}
          />
        ))}
      </section>

      <section className="card">
        <h3>Pilares SSI-JS</h3>
        {result.ssiJs.pillars.map((p) => (
          <ScoreBar
            key={p.id}
            label={p.label}
            score={p.score}
            maxScore={p.maxScore}
            feedback={p.feedback}
          />
        ))}
      </section>

      {result.topGaps.length > 0 && (
        <section className="card card--gaps">
          <h3>Principais gaps</h3>
          <ul>
            {result.topGaps.map((gap) => (
              <li key={gap}>{gap}</li>
            ))}
          </ul>
        </section>
      )}

      {result.weeklyActions.length > 0 && (
        <section className="card card--actions">
          <h3>Ações para esta semana</h3>
          <ol>
            {result.weeklyActions.map((action) => (
              <li key={action}>{action}</li>
            ))}
          </ol>
        </section>
      )}
    </div>
  )
}
