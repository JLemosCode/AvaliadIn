import type { CriterionScore } from '../types'

interface MarketFitPanelProps {
  score: number
  level: string
  criteria: CriterionScore[]
}

const CRITERIA_HINTS: Record<string, string> = {
  headline: 'Recrutadores e IA de hiring leem primeiro a headline na busca booleana.',
  about: 'O hook do Sobre define se o perfil passa no filtro de relevância.',
  experience: 'Bullets com stack e resultados aumentam match em ATS e Recruiter Search.',
  skills: 'Skills limpas e fixadas alimentam algoritmos de ranking e endorsements.',
  consistency: 'Cluster coerente entre seções — sinal que sistemas de IA usam para confiança.',
  openToWork: 'Open to Work visível para recrutadores acelera convites InMail.',
  education: 'Formação alinhada reforça fit para vagas com requisito acadêmico.',
}

export function MarketFitPanel({ score, level, criteria }: MarketFitPanelProps) {
  const sorted = [...criteria].sort((a, b) => a.score / a.maxScore - b.score / b.maxScore)

  return (
    <section className="card market-fit">
      <header className="market-fit__header">
        <div>
          <h3>RDIS — Validação para busca de recrutadores</h3>
          <p>
            Simula o que <strong>Recruiter Search</strong>, ATS e assistentes de contratação
            avaliam no seu currículo LinkedIn — antes da rotina social do SSI.
          </p>
        </div>
        <div className="market-fit__score">
          <span className="market-fit__score-value">{score}</span>
          <span className="market-fit__score-max">/100</span>
          <span className="market-fit__score-level">{level}</span>
        </div>
      </header>

      <div className="market-fit__grid">
        {sorted.map((c) => {
          const pct = c.maxScore > 0 ? Math.round((c.score / c.maxScore) * 100) : 0
          const hint = CRITERIA_HINTS[c.id] ?? ''
          return (
            <article key={c.id} className="market-fit__criterion">
              <div className="market-fit__criterion-head">
                <strong>{c.label}</strong>
                <span>
                  {c.score}/{c.maxScore}
                </span>
              </div>
              <div className="market-fit__bar" aria-hidden="true">
                <div
                  className={`market-fit__bar-fill market-fit__bar-fill--${pct >= 70 ? 'ok' : pct >= 45 ? 'mid' : 'low'}`}
                  style={{ width: `${pct}%` }}
                />
              </div>
              {c.feedback ? (
                <p className="market-fit__feedback">{c.feedback}</p>
              ) : (
                <p className="market-fit__hint">{hint}</p>
              )}
            </article>
          )
        })}
      </div>

      <p className="market-fit__footnote">
        Perfil otimizado (RDIS alto) + rotina social (SSI alto) = descoberta no mercado. Veja{' '}
        <code>docs/05-ssi-vs-rdis.md</code> no repositório.
      </p>
    </section>
  )
}
