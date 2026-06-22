import { useCallback, useEffect, useState } from 'react'
import { evaluateCapturedProfile, getApiBase } from './api'
import { EvaluationReport } from './components/EvaluationReport'
import { HomePage } from './components/HomePage'
import { LoadingPanel } from './components/LoadingPanel'
import { PasteProfileCard } from './components/PasteProfileCard'
import { PdfUploadCard } from './components/PdfUploadCard'
import { ProfileForm } from './components/ProfileForm'
import {
  isExtensionInstalled,
  requestCaptureViaExtension,
  waitForExtension,
} from './lib/extensionBridge'
import { useLinkedInCapture } from './lib/linkedInCapture'
import { createEmptyRequest } from './sampleData'
import type { AppStep, LinkedInEvaluationResult, ProfileEvaluationRequest } from './types'
import './App.css'

function App() {
  const [step, setStep] = useState<AppStep>('home')
  const [linkedInUrl, setLinkedInUrl] = useState('')
  const [profileDraft, setProfileDraft] = useState<ProfileEvaluationRequest>(createEmptyRequest())
  const [evaluationData, setEvaluationData] = useState<LinkedInEvaluationResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [importWarnings, setImportWarnings] = useState<string[]>([])
  const [loadingPhase, setLoadingPhase] = useState<'evaluate' | 'ai'>('evaluate')
  const [extensionReady, setExtensionReady] = useState(isExtensionInstalled())

  useEffect(() => {
    waitForExtension(3000).then(setExtensionReady)
  }, [])

  const reset = () => {
    setStep('home')
    setLinkedInUrl('')
    setProfileDraft(createEmptyRequest())
    setEvaluationData(null)
    setError(null)
    setImportWarnings([])
  }

  const showReport = (data: LinkedInEvaluationResult) => {
    setEvaluationData(data)
    setStep('report')
    setLoading(false)
  }

  const runEvaluation = useCallback(
    async (profile: ProfileEvaluationRequest, sourceUrl: string) => {
      setLoading(true)
      setError(null)
      setLoadingPhase('evaluate')
      setStep('loading')
      try {
        const data = await evaluateCapturedProfile(profile, sourceUrl, {
          onPhase: (phase) => setLoadingPhase(phase),
        })
        showReport(data)
      } catch (err) {
        setLoading(false)
        setError(err instanceof Error ? err.message : 'Erro inesperado na avaliação')
        setStep('manual')
      }
    },
    [],
  )

  useLinkedInCapture(
    useCallback(
      (payload) => runEvaluation(payload.profile, payload.sourceUrl),
      [runEvaluation],
    ),
  )

  const handleStartManual = () => {
    setProfileDraft(createEmptyRequest())
    setError(null)
    setStep('manual')
  }

  const handlePasteProfile = () => {
    setError(null)
    setStep('paste')
  }

  const handlePdfImport = () => {
    setError(null)
    setStep('pdf')
  }

  const handleImportedProfile = (
    profile: ProfileEvaluationRequest,
    sourceUrl: string,
    warnings: string[],
  ) => {
    setProfileDraft(profile)
    setLinkedInUrl(sourceUrl)
    setImportWarnings(warnings)
    setError(null)
    setStep('manual')
  }

  const handleParsedPaste = (profile: ProfileEvaluationRequest) => {
    handleImportedProfile(profile, linkedInUrl.trim() || 'https://www.linkedin.com/in/perfil', [])
  }

  const handleSubmitManual = () => {
    const sourceUrl = linkedInUrl.trim() || 'https://www.linkedin.com/in/perfil'
    void runEvaluation(profileDraft, sourceUrl)
  }

  const handleExtensionCapture = async (url: string) => {
    setLinkedInUrl(url)
    setError(null)
    setLoading(true)
    setStep('loading')

    const hasExtension = extensionReady || (await waitForExtension(2000))
    setExtensionReady(hasExtension)

    if (!hasExtension) {
      setLoading(false)
      setStep('home')
      setError('Extensão não detectada. Use o formulário manual ou cole o texto do LinkedIn.')
      return
    }

    try {
      const data = await requestCaptureViaExtension(url, getApiBase())
      showReport(data)
    } catch (err) {
      setLoading(false)
      setStep('home')
      setError(
        err instanceof Error
          ? err.message
          : 'Falha na extensão. Use o formulário manual ou cole o texto do LinkedIn.',
      )
    }
  }

  return (
    <div className="app">
      <header className="header">
        <div className="header__inner">
          <div className="header__brand">
            <span className="header__logo">in</span>
            <div>
              <h1>AvaliadIN</h1>
              <p>Avaliação RDIS + SSI-JS alinhada ao LinkedIn SSI</p>
            </div>
          </div>
          {step !== 'home' && (
            <button type="button" className="header__link header__link--btn" onClick={reset}>
              Início
            </button>
          )}
        </div>
      </header>

      <main className="main">
        {step === 'home' && (
          <HomePage
            loading={loading}
            error={error}
            linkedInUrl={linkedInUrl}
            extensionReady={extensionReady}
            onStartManual={handleStartManual}
            onPasteProfile={handlePasteProfile}
            onPdfImport={handlePdfImport}
            onExtensionCapture={handleExtensionCapture}
          />
        )}

        {step === 'pdf' && (
          <div>
            <PdfUploadCard onImported={handleImportedProfile} />
            <button type="button" className="btn btn--ghost" onClick={() => setStep('home')}>
              ← Voltar
            </button>
          </div>
        )}

        {step === 'paste' && (
          <div>
            <PasteProfileCard onParsed={handleParsedPaste} />
            <button type="button" className="btn btn--ghost" onClick={() => setStep('home')}>
              ← Voltar
            </button>
          </div>
        )}

        {step === 'manual' && (
          <div>
            <section className="card import-card">
              <label>
                URL do perfil LinkedIn (opcional)
                <input
                  type="url"
                  value={linkedInUrl}
                  onChange={(e) => setLinkedInUrl(e.target.value)}
                  placeholder="https://www.linkedin.com/in/seu-perfil"
                  disabled={loading}
                />
              </label>
            </section>
            <ProfileForm
              data={profileDraft}
              loading={loading}
              sourceUrl={linkedInUrl || undefined}
              importWarnings={importWarnings}
              onChange={setProfileDraft}
              onSubmit={handleSubmitManual}
              onBack={() => {
                setError(null)
                setStep('home')
              }}
            />
            {error && <div className="alert alert--error">{error}</div>}
          </div>
        )}

        {step === 'loading' && <LoadingPanel phase={loadingPhase} />}

        {step === 'report' && evaluationData && (
          <EvaluationReport data={evaluationData} onNewEvaluation={reset} />
        )}
      </main>

      <footer className="footer">
        <p>
          AvaliadIN — método baseado em{' '}
          <a href="https://www.linkedin.com/sales/ssi" target="_blank" rel="noreferrer">
            LinkedIn SSI
          </a>
        </p>
      </footer>
    </div>
  )
}

export default App
