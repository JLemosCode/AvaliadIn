import { useState } from 'react'
import { parsePastedLinkedInText } from '../lib/parsePastedProfile'
import type { ProfileEvaluationRequest } from '../types'

interface PasteProfileCardProps {
  onParsed: (profile: ProfileEvaluationRequest) => void
}

export function PasteProfileCard({ onParsed }: PasteProfileCardProps) {
  const [text, setText] = useState('')
  const [error, setError] = useState<string | null>(null)

  const handleParse = () => {
    setError(null)
    const profile = parsePastedLinkedInText(text)
    if (!profile.headline.trim() && !profile.about.trim()) {
      setError(
        'Não encontramos headline ou sobre no texto. Selecione e copie as seções do perfil no LinkedIn (Ctrl+C).',
      )
      return
    }
    onParsed(profile)
  }

  return (
    <section className="card paste-profile">
      <h2>Colar do LinkedIn</h2>
      <p>
        Abra seu perfil no LinkedIn, selecione o texto das seções (headline, sobre, experiências) e
        cole abaixo. Funciona sem script, extensão ou cookie.
      </p>
      <ol className="steps paste-profile__steps">
        <li>
          <span>1</span> No LinkedIn, abra <strong>Ver perfil completo</strong>
        </li>
        <li>
          <span>2</span> Selecione o conteúdo (ou seção por seção) e copie (<strong>Ctrl+C</strong>)
        </li>
        <li>
          <span>3</span> Cole aqui e clique em <strong>Importar texto</strong>
        </li>
      </ol>
      <textarea
        className="paste-profile__textarea"
        rows={8}
        placeholder="Cole aqui o texto copiado do LinkedIn (headline, Sobre, Experiência...)"
        value={text}
        onChange={(e) => setText(e.target.value)}
      />
      <button
        type="button"
        className="btn btn--primary"
        onClick={handleParse}
        disabled={!text.trim()}
      >
        Importar texto
      </button>
      {error && <div className="alert alert--error">{error}</div>}
    </section>
  )
}
