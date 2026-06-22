import type { ExperienceInput, ProfileEvaluationRequest } from '../types'

interface ProfileFormProps {
  data: ProfileEvaluationRequest
  loading: boolean
  sourceUrl?: string
  importWarnings?: string[]
  onChange: (data: ProfileEvaluationRequest) => void
  onSubmit: () => void
  onBack?: () => void
}

function linesToList(value: string): string[] {
  return value
    .split(/[\n,]/)
    .map((s) => s.trim())
    .filter(Boolean)
}

function listToLines(items: string[]): string {
  return items.join('\n')
}

export function ProfileForm({
  data,
  loading,
  sourceUrl,
  importWarnings,
  onChange,
  onSubmit,
  onBack,
}: ProfileFormProps) {
  const update = (patch: Partial<ProfileEvaluationRequest>) =>
    onChange({ ...data, ...patch })

  const updateExperience = (index: number, patch: Partial<ExperienceInput>) => {
    const experiences = data.experiences.map((exp, i) =>
      i === index ? { ...exp, ...patch } : exp,
    )
    update({ experiences })
  }

  const addExperience = () =>
    update({
      experiences: [...data.experiences, { title: '', company: '', description: '' }],
    })

  const removeExperience = (index: number) => {
    if (data.experiences.length <= 1) return
    update({ experiences: data.experiences.filter((_, i) => i !== index) })
  }

  return (
    <form
      className="form"
      onSubmit={(e) => {
        e.preventDefault()
        onSubmit()
      }}
    >
      <div className="form__toolbar">
        {onBack && (
          <button type="button" className="btn btn--ghost" onClick={onBack}>
            ← Voltar
          </button>
        )}
      </div>

      {sourceUrl && (
        <div className="import-banner card">
          <strong>Perfil importado</strong>
          <a href={sourceUrl} target="_blank" rel="noreferrer">{sourceUrl}</a>
          {data.experiences.some((e) => e.title.trim() || e.company.trim()) && (
            <p className="import-banner__meta">
              {data.experiences.filter((e) => e.title.trim() || e.company.trim()).length}{' '}
              experiência(s) preenchida(s) — adicione mais se o PDF tiver histórico em várias páginas.
            </p>
          )}
        </div>
      )}

      {importWarnings && importWarnings.length > 0 && (
        <div className="alert alert--warning">
          <strong>Atenção na importação</strong>
          <ul>
            {importWarnings.map((w) => (
              <li key={w}>{w}</li>
            ))}
          </ul>
        </div>
      )}

      <section className="card">
        <h3>Perfil</h3>
        <label>
          Cargo alvo
          <input
            type="text"
            value={data.targetRole}
            onChange={(e) => update({ targetRole: e.target.value })}
          />
        </label>
        <label>
          Headline
          <input
            type="text"
            value={data.headline}
            onChange={(e) => update({ headline: e.target.value })}
            placeholder="Ex.: Desenvolvedor Full Stack Sênior | C# .NET | 10+ anos"
          />
        </label>
        <label>
          Sobre (About)
          <textarea
            rows={5}
            value={data.about}
            onChange={(e) => update({ about: e.target.value })}
            placeholder="Resumo profissional com keywords do cargo alvo..."
          />
        </label>
      </section>

      <section className="card">
        <div className="card__row">
          <h3>Experiências</h3>
          <button type="button" className="btn btn--ghost btn--sm" onClick={addExperience}>
            + Adicionar
          </button>
        </div>
        {data.experiences.map((exp, index) => (
          <div key={index} className="experience-block">
            <div className="card__row">
              <span className="experience-block__title">Experiência {index + 1}</span>
              {data.experiences.length > 1 && (
                <button
                  type="button"
                  className="btn btn--ghost btn--sm"
                  onClick={() => removeExperience(index)}
                >
                  Remover
                </button>
              )}
            </div>
            <label>
              Título
              <input
                type="text"
                value={exp.title}
                onChange={(e) => updateExperience(index, { title: e.target.value })}
              />
            </label>
            <label>
              Empresa
              <input
                type="text"
                value={exp.company}
                onChange={(e) => updateExperience(index, { company: e.target.value })}
              />
            </label>
            <label>
              Descrição
              <textarea
                rows={3}
                value={exp.description}
                onChange={(e) => updateExperience(index, { description: e.target.value })}
              />
            </label>
          </div>
        ))}
      </section>

      <section className="card">
        <h3>Skills</h3>
        <label>
          Todas as skills (uma por linha)
          <textarea
            rows={4}
            value={listToLines(data.skills)}
            onChange={(e) => update({ skills: linesToList(e.target.value) })}
          />
        </label>
        <label>
          Skills fixadas no topo (uma por linha)
          <textarea
            rows={2}
            value={listToLines(data.pinnedSkills)}
            onChange={(e) => update({ pinnedSkills: linesToList(e.target.value) })}
          />
        </label>
      </section>

      <section className="card">
        <h3>Open to Work</h3>
        <div className="checkbox-grid">
          <label className="checkbox">
            <input
              type="checkbox"
              checked={data.openToWork.enabled}
              onChange={(e) =>
                update({ openToWork: { ...data.openToWork, enabled: e.target.checked } })
              }
            />
            Ativado
          </label>
          <label className="checkbox">
            <input
              type="checkbox"
              checked={data.openToWork.recruitersOnly}
              onChange={(e) =>
                update({
                  openToWork: { ...data.openToWork, recruitersOnly: e.target.checked },
                })
              }
            />
            Só recrutadores
          </label>
          <label className="checkbox">
            <input
              type="checkbox"
              checked={data.openToWork.remote}
              onChange={(e) =>
                update({ openToWork: { ...data.openToWork, remote: e.target.checked } })
              }
            />
            Remoto
          </label>
          <label className="checkbox">
            <input
              type="checkbox"
              checked={data.openToWork.contract}
              onChange={(e) =>
                update({ openToWork: { ...data.openToWork, contract: e.target.checked } })
              }
            />
            Contrato/PJ
          </label>
          <label className="checkbox">
            <input
              type="checkbox"
              checked={data.openToWork.fullTime}
              onChange={(e) =>
                update({ openToWork: { ...data.openToWork, fullTime: e.target.checked } })
              }
            />
            CLT
          </label>
        </div>
        <label>
          Títulos alvo (um por linha)
          <textarea
            rows={2}
            value={listToLines(data.openToWork.targetTitles)}
            onChange={(e) =>
              update({
                openToWork: {
                  ...data.openToWork,
                  targetTitles: linesToList(e.target.value),
                },
              })
            }
          />
        </label>
      </section>

      <section className="card">
        <h3>Atividade (SSI-JS)</h3>
        <div className="field-grid">
          <label>
            Posts (últimos 90 dias)
            <input
              type="number"
              min={0}
              value={data.activity.postsLast90Days}
              onChange={(e) =>
                update({
                  activity: {
                    ...data.activity,
                    postsLast90Days: Number(e.target.value),
                  },
                })
              }
            />
          </label>
          <label>
            Comentários/semana
            <input
              type="number"
              min={0}
              value={data.activity.commentsPerWeek}
              onChange={(e) =>
                update({
                  activity: {
                    ...data.activity,
                    commentsPerWeek: Number(e.target.value),
                  },
                })
              }
            />
          </label>
          <label>
            Convites/semana
            <input
              type="number"
              min={0}
              value={data.activity.invitesPerWeek}
              onChange={(e) =>
                update({
                  activity: {
                    ...data.activity,
                    invitesPerWeek: Number(e.target.value),
                  },
                })
              }
            />
          </label>
          <label>
            Visualizações/dia
            <input
              type="number"
              min={0}
              value={data.activity.profileViewsPerDay}
              onChange={(e) =>
                update({
                  activity: {
                    ...data.activity,
                    profileViewsPerDay: Number(e.target.value),
                  },
                })
              }
            />
          </label>
          <label>
            Buscas/semana
            <input
              type="number"
              min={0}
              value={data.activity.searchesPerWeek}
              onChange={(e) =>
                update({
                  activity: {
                    ...data.activity,
                    searchesPerWeek: Number(e.target.value),
                  },
                })
              }
            />
          </label>
        </div>
        <div className="checkbox-grid">
          <label className="checkbox">
            <input
              type="checkbox"
              checked={data.activity.respondsToInMail}
              onChange={(e) =>
                update({
                  activity: {
                    ...data.activity,
                    respondsToInMail: e.target.checked,
                  },
                })
              }
            />
            Responde InMail
          </label>
          <label className="checkbox">
            <input
              type="checkbox"
              checked={data.activity.creatorMode}
              onChange={(e) =>
                update({
                  activity: { ...data.activity, creatorMode: e.target.checked },
                })
              }
            />
            Creator mode
          </label>
        </div>
      </section>

      <section className="card">
        <h3>Completude do perfil</h3>
        <div className="checkbox-grid">
          <label className="checkbox">
            <input
              type="checkbox"
              checked={data.completeness.hasPhoto}
              onChange={(e) =>
                update({
                  completeness: {
                    ...data.completeness,
                    hasPhoto: e.target.checked,
                  },
                })
              }
            />
            Foto
          </label>
          <label className="checkbox">
            <input
              type="checkbox"
              checked={data.completeness.hasBanner}
              onChange={(e) =>
                update({
                  completeness: {
                    ...data.completeness,
                    hasBanner: e.target.checked,
                  },
                })
              }
            />
            Banner
          </label>
          <label className="checkbox">
            <input
              type="checkbox"
              checked={data.completeness.hasFeatured}
              onChange={(e) =>
                update({
                  completeness: {
                    ...data.completeness,
                    hasFeatured: e.target.checked,
                  },
                })
              }
            />
            Destaques
          </label>
        </div>
        <div className="field-grid">
          <label>
            Recomendações
            <input
              type="number"
              min={0}
              value={data.completeness.recommendations}
              onChange={(e) =>
                update({
                  completeness: {
                    ...data.completeness,
                    recommendations: Number(e.target.value),
                  },
                })
              }
            />
          </label>
          <label>
            Endossos (top skills)
            <input
              type="number"
              min={0}
              value={data.completeness.endorsementsTopSkills}
              onChange={(e) =>
                update({
                  completeness: {
                    ...data.completeness,
                    endorsementsTopSkills: Number(e.target.value),
                  },
                })
              }
            />
          </label>
        </div>
      </section>

      <button type="submit" className="btn btn--primary btn--submit" disabled={loading}>
        {loading ? 'Gerando relatório...' : 'Avaliar currículo'}
      </button>
    </form>
  )
}
