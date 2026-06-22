import type { FormEvent } from 'react'
import { isExtensionInstalled } from '../lib/extensionBridge'

interface HomePageProps {
  loading: boolean
  error: string | null
  linkedInUrl: string
  extensionReady: boolean
  onStartManual: () => void
  onPasteProfile: () => void
  onPdfImport: () => void
  onExtensionCapture: (url: string) => void
}

export function HomePage({
  loading,
  error,
  linkedInUrl,
  extensionReady,
  onStartManual,
  onPasteProfile,
  onPdfImport,
  onExtensionCapture,
}: HomePageProps) {
  const hasExtension = extensionReady || isExtensionInstalled()

  const handleExtensionSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const form = new FormData(e.currentTarget)
    const url = String(form.get('linkedinUrl') ?? '')
    onExtensionCapture(url)
  }

  return (
    <div className="home">
      <section className="hero-card">
        <h2>Melhore seu currículo para o mercado</h2>
        <p>
          Avaliamos seu perfil como recrutadores e IA de contratação veem — e mostramos um painel
          no modelo do{' '}
          <a href="https://www.linkedin.com/sales/ssi" target="_blank" rel="noreferrer">
            LinkedIn SSI
          </a>
          .
        </p>
        <div className="hero-card__scores">
          <article>
            <h3>RDIS</h3>
            <p>
              Otimização para <strong>Recruiter Search</strong> — keywords, estrutura e fit para
              recrutadores.
            </p>
          </article>
          <article>
            <h3>SSI-JS</h3>
            <p>
              Adaptação dos{' '}
              <a href="https://www.linkedin.com/sales/ssi" target="_blank" rel="noreferrer">
                4 pilares do LinkedIn SSI
              </a>{' '}
              para job seekers.
            </p>
          </article>
          <article>
            <h3>Score combinado</h3>
            <p>
              <code>(RDIS × 0,6) + (SSI-JS × 0,4)</code>
            </p>
          </article>
        </div>
      </section>

      <section className="card home-methods">
        <h2>Como avaliar</h2>
        <div className="home-methods__grid home-methods__grid--3">
          <article className="home-methods__item home-methods__item--primary">
            <h3>1. PDF do LinkedIn</h3>
            <p>
              Perfil → <strong>Mais</strong> → <strong>Salvar em PDF</strong>. Enviamos o arquivo e
              importamos headline, sobre, experiências e skills.
            </p>
            <button type="button" className="btn btn--primary" onClick={onPdfImport} disabled={loading}>
              Importar PDF
            </button>
          </article>
          <article className="home-methods__item">
            <h3>2. Colar texto</h3>
            <p>Selecione o perfil no LinkedIn, copie (Ctrl+C) e importamos o texto.</p>
            <button type="button" className="btn btn--ghost" onClick={onPasteProfile} disabled={loading}>
              Colar texto
            </button>
          </article>
          <article className="home-methods__item">
            <h3>3. Formulário</h3>
            <p>Preencha headline, sobre e experiências manualmente.</p>
            <button type="button" className="btn btn--ghost" onClick={onStartManual} disabled={loading}>
              Abrir formulário
            </button>
          </article>
        </div>
      </section>

      {hasExtension && (
        <section className="card import-card">
          <h2>Captura automática (extensão)</h2>
          <p className="alert alert--success">Extensão AvaliadIN detectada.</p>
          <form className="import-form" onSubmit={handleExtensionSubmit}>
            <label>
              URL do perfil LinkedIn
              <input
                name="linkedinUrl"
                type="url"
                required
                defaultValue={linkedInUrl}
                placeholder="https://www.linkedin.com/in/seu-perfil"
                disabled={loading}
              />
            </label>
            <button type="submit" className="btn btn--primary" disabled={loading}>
              {loading ? 'Capturando...' : 'Capturar e avaliar'}
            </button>
          </form>
        </section>
      )}

      {error && <div className="alert alert--error">{error}</div>}
    </div>
  )
}
