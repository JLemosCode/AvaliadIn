import type { ProfileEvaluationRequest } from '../types'

interface CapturedProfileSummaryProps {
  profile: ProfileEvaluationRequest
  sourceUrl: string
  importWarnings?: string[]
}

export function CapturedProfileSummary({
  profile,
  sourceUrl,
  importWarnings,
}: CapturedProfileSummaryProps) {
  const experiences = profile.experiences.filter((e) => e.title.trim())
  const skillsPreview = profile.skills.slice(0, 8)

  return (
    <section className="card captured-summary">
      <h3>Perfil capturado</h3>
      <p className="captured-summary__url">
        <a href={sourceUrl} target="_blank" rel="noreferrer">{sourceUrl}</a>
      </p>

      {profile.headline && (
        <div className="captured-summary__field">
          <strong>Headline</strong>
          <p>{profile.headline}</p>
        </div>
      )}

      {profile.about && (
        <div className="captured-summary__field">
          <strong>Sobre</strong>
          <p>{profile.about.length > 280 ? `${profile.about.slice(0, 280)}…` : profile.about}</p>
        </div>
      )}

      {experiences.length > 0 && (
        <div className="captured-summary__field">
          <strong>Experiências ({experiences.length})</strong>
          <ul>
            {experiences.slice(0, 4).map((exp, i) => (
              <li key={i}>
                {exp.title}
                {exp.company ? ` — ${exp.company}` : ''}
              </li>
            ))}
          </ul>
        </div>
      )}

      {skillsPreview.length > 0 && (
        <div className="captured-summary__field">
          <strong>Skills</strong>
          <p>{skillsPreview.join(' · ')}{profile.skills.length > 8 ? ' …' : ''}</p>
        </div>
      )}

      {importWarnings && importWarnings.length > 0 && (
        <div className="alert alert--warning">
          <ul>
            {importWarnings.map((w) => (
              <li key={w}>{w}</li>
            ))}
          </ul>
        </div>
      )}
    </section>
  )
}
