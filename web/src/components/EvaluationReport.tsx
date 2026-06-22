import type { LinkedInEvaluationResult } from '../types'
import { AiInsightsPanel } from './AiInsightsPanel'
import { CapturedProfileSummary } from './CapturedProfileSummary'
import { MarketFitPanel } from './MarketFitPanel'
import { ScoreGauge } from './ScoreGauge'
import { SsiDashboard } from './SsiDashboard'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

interface EvaluationReportProps {
  data: LinkedInEvaluationResult
  onNewEvaluation: () => void
}

export function EvaluationReport({ data, onNewEvaluation }: EvaluationReportProps) {
  const result = data.evaluation

  const combinedChartData = [
    { name: 'RDIS (mercado)', score: result.rdis.score, fill: '#0a66c2' },
    { name: 'SSI-JS', score: result.ssiJs.score, fill: '#057642' },
    { name: 'Combinado', score: result.combined.score, fill: '#915907' },
  ]

  return (
    <div className="report">
      <header className="report__header">
        <div>
          <h2>Seu índice AvaliadIN</h2>
          <p className="report__subtitle">
            Validação de currículo para o mercado (RDIS) + índice social no modelo{' '}
            <a href="https://www.linkedin.com/sales/ssi" target="_blank" rel="noreferrer">
              LinkedIn SSI
            </a>
            . Use o plano de ação e a IA para melhorar descoberta por recrutadores.
          </p>
        </div>
        <button type="button" className="btn btn--ghost" onClick={onNewEvaluation}>
          Nova avaliação
        </button>
      </header>

      <CapturedProfileSummary
        profile={data.profile}
        sourceUrl={data.sourceUrl}
        importWarnings={data.importWarnings}
      />

      <div className="report__hero report__hero--compact">
        <ScoreGauge
          label="Score combinado"
          score={result.combined.score}
          level={result.combined.level}
          accent="combined"
        />
        <div className="report__hero-text">
          <h3>{result.combined.level}</h3>
          <p>
            <strong>RDIS {result.rdis.score}</strong> — fit para buscas de recrutadores e IA de
            hiring. <strong>SSI-JS {result.ssiJs.score}</strong> — pilares do{' '}
            <a href="https://www.linkedin.com/sales/ssi" target="_blank" rel="noreferrer">
              SSI oficial
            </a>
            .
          </p>
        </div>
      </div>

      <SsiDashboard
        score={result.ssiJs.score}
        level={result.ssiJs.level}
        pillars={result.ssiJs.pillars}
        partialScore={result.ssiJs.partialScore}
        benchmark={result.benchmark}
      />

      <MarketFitPanel
        score={result.rdis.score}
        level={result.rdis.level}
        criteria={result.rdis.criteria}
      />

      <section className="card chart-card">
        <h3>Visão geral</h3>
        <div className="chart-container">
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={combinedChartData} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#eee" />
              <XAxis dataKey="name" tick={{ fontSize: 12 }} />
              <YAxis domain={[0, 100]} tick={{ fontSize: 12 }} />
              <Tooltip formatter={(value) => [`${value}/100`, 'Score']} />
              <Bar dataKey="score" radius={[8, 8, 0, 0]}>
                {combinedChartData.map((entry) => (
                  <Cell key={entry.name} fill={entry.fill} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      </section>

      {result.topGaps.length > 0 && (
        <section className="card card--tips">
          <h3>Principais gaps</h3>
          <ul className="tips-list">
            {result.topGaps.map((gap) => (
              <li key={gap}>{gap}</li>
            ))}
          </ul>
        </section>
      )}

      {result.weeklyActions.length > 0 && (
        <section className="card card--actions">
          <h3>Plano de ação — esta semana</h3>
          <ol className="action-list">
            {result.weeklyActions.map((action) => (
              <li key={action}>{action}</li>
            ))}
          </ol>
        </section>
      )}

      <AiInsightsPanel
        profile={data.profile}
        evaluation={result}
        initialInsights={data.aiInsights}
        autoLoaded={Boolean(data.aiInsights)}
      />

      {result.warnings.length > 0 && (
        <section className="card">
          <h3>Observações</h3>
          <ul>
            {result.warnings.map((w) => (
              <li key={w}>{w}</li>
            ))}
          </ul>
        </section>
      )}
    </div>
  )
}
