import type { PillarScore, SsiBenchmark } from '../types'

const PILLAR_META: Record<
  string,
  { title: string; subtitle: string; tip: string }
> = {
  brand: {
    title: 'Estabelecer sua marca profissional',
    subtitle: 'Headline, sobre, foto e destaques que recrutadores veem primeiro.',
    tip: 'Alinhe headline e sobre com o cargo alvo e stack do mercado.',
  },
  find: {
    title: 'Encontrar as pessoas certas',
    subtitle: 'Buscas e visualizações de perfis de recrutadores e líderes técnicos.',
    tip: '10+ buscas/semana em vagas e perfis de hiring managers.',
  },
  engage: {
    title: 'Interagir com insights',
    subtitle: 'Posts e comentários substantivos nos últimos 90 dias.',
    tip: '1 post técnico/semana + comentários de 3+ linhas em posts do nicho.',
  },
  relationships: {
    title: 'Criar relacionamentos',
    subtitle: 'Convites personalizados e respostas a InMail de recrutadores.',
    tip: '3–5 convites/dia com nota personalizada; responda InMail em 48h.',
  },
}

interface SsiDashboardProps {
  score: number
  level: string
  pillars: PillarScore[]
  partialScore: boolean
  benchmark?: SsiBenchmark
}

function pillarStatus(score: number, max: number) {
  const pct = max > 0 ? score / max : 0
  if (pct >= 0.75) return { label: 'Forte', tone: 'strong' as const }
  if (pct >= 0.45) return { label: 'Em progresso', tone: 'mid' as const }
  return { label: 'Atenção', tone: 'weak' as const }
}

export function SsiDashboard({ score, level, pillars, partialScore, benchmark }: SsiDashboardProps) {
  const pillarBenchmarks = benchmark?.pillars ?? []

  return (
    <section className="ssi-dashboard">
      <div className="ssi-dashboard__hero">
        <div className="ssi-dashboard__score-ring">
          <ScoreRing value={score} max={100} />
          <div className="ssi-dashboard__score-text">
            <span className="ssi-dashboard__score-value">{score}</span>
            <span className="ssi-dashboard__score-label">SSI-JS</span>
            <span className="ssi-dashboard__score-level">{level}</span>
          </div>
        </div>
        <div className="ssi-dashboard__intro">
          <h3>Índice social para job seekers</h3>
          {benchmark && (
            <div className="ssi-benchmark-hero">
              <span className="ssi-benchmark-hero__percentile">
                À frente de ~{benchmark.percentile}% da rede
              </span>
              <p className="ssi-benchmark-hero__summary">{benchmark.comparisonSummary}</p>
              <p className="ssi-benchmark-hero__segment">{benchmark.segmentLabel}</p>
            </div>
          )}
          <p>
            Mesma estrutura dos{' '}
            <a href="https://www.linkedin.com/sales/ssi" target="_blank" rel="noreferrer">
              4 pilares do LinkedIn SSI
            </a>
            : marca, encontrar, engajar e relacionar. O SSI oficial mede sua atividade real nos
            últimos 90 dias — use este painel para planejar e compare em{' '}
            <a href="https://www.linkedin.com/sales/ssi" target="_blank" rel="noreferrer">
              linkedin.com/sales/ssi
            </a>
            .
          </p>
          {partialScore && (
            <p className="ssi-dashboard__partial">
              Score parcial: preencha atividade e completude no formulário para aproximar do SSI
              oficial.
            </p>
          )}
        </div>
      </div>

      {benchmark && (
        <div className="ssi-benchmark-compare">
          <h4 className="ssi-benchmark-compare__title">Você vs. rede vs. topo do setor</h4>
          <BenchmarkBar label="Você" value={benchmark.yourScore} max={100} tone="you" />
          <BenchmarkBar label="Média da rede (est.)" value={benchmark.networkAverage} max={100} tone="network" />
          <BenchmarkBar label="Topo do setor (est.)" value={benchmark.industryTop} max={100} tone="top" />
          <p className="ssi-benchmark-compare__note">{benchmark.disclaimer}</p>
        </div>
      )}

      <div className="ssi-dashboard__pillars">
        {pillars.map((pillar) => {
          const meta = PILLAR_META[pillar.id] ?? {
            title: pillar.label,
            subtitle: '',
            tip: '',
          }
          const bench = pillarBenchmarks.find((b) => b.id === pillar.id)
          const status = pillarStatus(pillar.score, pillar.maxScore)
          const pct = pillar.maxScore > 0 ? Math.round((pillar.score / pillar.maxScore) * 100) : 0

          return (
            <article key={pillar.id} className={`ssi-pillar ssi-pillar--${status.tone}`}>
              <header className="ssi-pillar__header">
                <div>
                  <h4>{meta.title}</h4>
                  <p>{meta.subtitle}</p>
                </div>
                <div className="ssi-pillar__points">
                  <strong>{pillar.score}</strong>
                  <span>/{pillar.maxScore}</span>
                </div>
              </header>
              {bench && (
                <p className="ssi-pillar__vs">
                  Rede ~{bench.networkAverage} · Topo ~{bench.industryTop}
                </p>
              )}
              <div className="ssi-pillar__bar" aria-hidden="true">
                <div className="ssi-pillar__bar-fill" style={{ width: `${pct}%` }} />
              </div>
              <footer className="ssi-pillar__footer">
                <span className={`ssi-pillar__badge ssi-pillar__badge--${status.tone}`}>
                  {status.label}
                </span>
                {pillar.feedback ? (
                  <p className="ssi-pillar__feedback">{pillar.feedback}</p>
                ) : (
                  <p className="ssi-pillar__tip">{meta.tip}</p>
                )}
              </footer>
            </article>
          )
        })}
      </div>
    </section>
  )
}

function ScoreRing({ value, max }: { value: number; max: number }) {
  const pct = Math.min(100, Math.round((value / max) * 100))
  const r = 52
  const circumference = 2 * Math.PI * r
  const offset = circumference - (pct / 100) * circumference

  return (
    <svg className="ssi-dashboard__ring" viewBox="0 0 120 120" aria-hidden="true">
      <circle className="ssi-dashboard__ring-track" cx="60" cy="60" r={r} />
      <circle
        className="ssi-dashboard__ring-fill"
        cx="60"
        cy="60"
        r={r}
        style={{
          strokeDasharray: circumference,
          strokeDashoffset: offset,
        }}
      />
    </svg>
  )
}

function BenchmarkBar({
  label,
  value,
  max,
  tone,
}: {
  label: string
  value: number
  max: number
  tone: 'you' | 'network' | 'top'
}) {
  const pct = max > 0 ? Math.round((value / max) * 100) : 0
  return (
    <div className={`ssi-benchmark-bar ssi-benchmark-bar--${tone}`}>
      <div className="ssi-benchmark-bar__head">
        <span>{label}</span>
        <strong>{value}</strong>
      </div>
      <div className="ssi-benchmark-bar__track">
        <div className="ssi-benchmark-bar__fill" style={{ width: `${pct}%` }} />
      </div>
    </div>
  )
}
