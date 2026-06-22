interface ScoreGaugeProps {
  label: string
  score: number
  maxScore?: number
  level?: string
  accent?: 'primary' | 'secondary' | 'combined'
}

const ACCENTS = {
  primary: '#0a66c2',
  secondary: '#057642',
  combined: '#915907',
} as const

export function ScoreGauge({
  label,
  score,
  maxScore = 100,
  level,
  accent = 'primary',
}: ScoreGaugeProps) {
  const pct = Math.min(100, Math.round((score / maxScore) * 100))
  const color = ACCENTS[accent]
  const circumference = 2 * Math.PI * 54
  const offset = circumference - (pct / 100) * circumference

  return (
    <div className="score-gauge">
      <svg viewBox="0 0 120 120" aria-hidden="true">
        <circle className="score-gauge__track" cx="60" cy="60" r="54" />
        <circle
          className="score-gauge__fill"
          cx="60"
          cy="60"
          r="54"
          stroke={color}
          style={{
            strokeDasharray: circumference,
            strokeDashoffset: offset,
          }}
        />
      </svg>
      <div className="score-gauge__content">
        <span className="score-gauge__value">{score}</span>
        <span className="score-gauge__max">/{maxScore}</span>
      </div>
      <p className="score-gauge__label">{label}</p>
      {level && <span className="score-gauge__level">{level}</span>}
    </div>
  )
}
