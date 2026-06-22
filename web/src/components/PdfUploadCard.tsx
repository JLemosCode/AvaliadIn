import { useRef, useState } from 'react'
import { importLinkedInPdf } from '../api'
import type { ProfileEvaluationRequest } from '../types'

interface PdfUploadCardProps {
  onImported: (profile: ProfileEvaluationRequest, sourceUrl: string, warnings: string[]) => void
}

export function PdfUploadCard({ onImported }: PdfUploadCardProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fileName, setFileName] = useState<string | null>(null)

  const handleFile = async (file: File | null) => {
    if (!file) return
    setFileName(file.name)
    setLoading(true)
    setError(null)

    try {
      const result = await importLinkedInPdf(file)
      onImported(result.profile, result.sourceUrl, result.warnings)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao importar PDF')
    } finally {
      setLoading(false)
    }
  }

  return (
    <section className="card paste-profile">
      <h2>Importar PDF do LinkedIn</h2>
      <p>
        Exporte seu perfil no LinkedIn e envie o arquivo aqui. Extraímos headline, sobre, experiências
        e skills para a avaliação RDIS + SSI-JS.
      </p>
      <ol className="steps paste-profile__steps">
        <li>
          <span>1</span> No LinkedIn: seu perfil → <strong>Mais</strong> →{' '}
          <strong>Salvar em PDF</strong> (ou <em>Save to PDF</em>)
        </li>
        <li>
          <span>2</span> Selecione o arquivo <code>.pdf</code> abaixo
        </li>
        <li>
          <span>3</span> Revise os dados importados e clique em <strong>Avaliar currículo</strong>
        </li>
      </ol>

      <input
        ref={inputRef}
        type="file"
        accept=".pdf,application/pdf"
        className="pdf-upload__input"
        onChange={(e) => void handleFile(e.target.files?.[0] ?? null)}
        disabled={loading}
      />

      <div className="pdf-upload__actions">
        <button
          type="button"
          className="btn btn--primary"
          onClick={() => inputRef.current?.click()}
          disabled={loading}
        >
          {loading ? 'Lendo PDF...' : fileName ? `Trocar arquivo (${fileName})` : 'Selecionar PDF'}
        </button>
      </div>

      <p className="paste-profile__hint">
        Atividade (SSI) e Open to Work não vêm no PDF — complete no formulário se quiser score SSI-JS
        mais preciso. Compare depois com o{' '}
        <a href="https://www.linkedin.com/sales/ssi" target="_blank" rel="noreferrer">
          LinkedIn SSI
        </a>
        .
      </p>

      {error && <div className="alert alert--error">{error}</div>}
    </section>
  )
}
