import { useState } from 'react'
import { parseCapturedJson, useCaptureSnippet } from '../lib/linkedInCapture'

interface CaptureGuideProps {
  linkedInUrl: string
  onCaptured: (raw: string) => void
  onBack: () => void
  error: string | null
}

export function CaptureGuide({ linkedInUrl, onCaptured, onBack, error }: CaptureGuideProps) {
  const { snippet, loading: snippetLoading } = useCaptureSnippet()
  const [pasteValue, setPasteValue] = useState('')
  const [copied, setCopied] = useState(false)
  const [localError, setLocalError] = useState<string | null>(null)

  const openLinkedIn = () => {
    window.open(linkedInUrl, 'avaliadin_linkedin')
  }

  const copyScript = async () => {
    if (!snippet) return
    await navigator.clipboard.writeText(snippet)
    setCopied(true)
    setTimeout(() => setCopied(false), 2500)
  }

  const handlePasteSubmit = () => {
    setLocalError(null)
    try {
      parseCapturedJson(pasteValue)
      onCaptured(pasteValue)
    } catch (err) {
      setLocalError(err instanceof Error ? err.message : 'JSON inválido')
    }
  }

  return (
    <div className="capture-guide">
      <section className="card">
        <div className="capture-guide__header">
          <h2>Capturar perfil no LinkedIn</h2>
          <button type="button" className="btn btn--ghost btn--sm" onClick={onBack}>
            ← Voltar
          </button>
        </div>
        <p>
          O LinkedIn só libera o conteúdo completo para quem está <strong>logado no navegador</strong>.
          Por isso a captura é feita no seu Chrome/Edge — sem API paga e sem armazenar sua senha.
        </p>
        <p className="capture-guide__url">
          Perfil: <a href={linkedInUrl} target="_blank" rel="noreferrer">{linkedInUrl}</a>
        </p>
      </section>

      <section className="card">
        <h3>Passo a passo</h3>
        <ol className="steps">
          <li>
            <span>1</span>
            <div>
              <strong>Abra seu perfil</strong> no LinkedIn (mesma conta, já logado)
              <br />
              <button type="button" className="btn btn--primary btn--sm capture-guide__open" onClick={openLinkedIn}>
                Abrir perfil no LinkedIn
              </button>
            </div>
          </li>
          <li>
            <span>2</span>
            <div>
              <strong>Copie o script</strong> e cole no Console do navegador (F12 → Console → Enter)
              <br />
              <button
                type="button"
                className="btn btn--ghost btn--sm"
                onClick={copyScript}
                disabled={snippetLoading || !snippet}
              >
                {copied ? 'Copiado!' : snippetLoading ? 'Carregando...' : 'Copiar script de captura'}
              </button>
            </div>
          </li>
          <li>
            <span>3</span>
            <div>
              O script envia os dados automaticamente para esta aba do AvaliadIN e inicia a avaliação.
            </div>
          </li>
        </ol>
      </section>

      <section className="card">
        <h3>Alternativa: colar JSON</h3>
        <p className="capture-guide__hint">
          Se o envio automático não funcionar, o script copia o JSON — cole abaixo:
        </p>
        <textarea
          className="capture-guide__paste"
          rows={6}
          placeholder='{"profile":{...},"sourceUrl":"https://www.linkedin.com/in/..."}'
          value={pasteValue}
          onChange={(e) => setPasteValue(e.target.value)}
        />
        <button
          type="button"
          className="btn btn--primary"
          onClick={handlePasteSubmit}
          disabled={!pasteValue.trim()}
        >
          Avaliar dados colados
        </button>
        {localError && <div className="alert alert--error">{localError}</div>}
      </section>

      {error && <div className="alert alert--error">{error}</div>}

      <p className="capture-guide__waiting">
        Aguardando captura do LinkedIn… mantenha esta aba aberta.
      </p>

      <p className="capture-guide__hint">
        Erros no Console como <code>realtime/connect 401</code> ou{' '}
        <code>chrome-extension://invalid</code> são do próprio LinkedIn/extensões do navegador
        — pode ignorar se o script mostrar “Perfil enviado” ou copiar o JSON.
      </p>
    </div>
  )
}
