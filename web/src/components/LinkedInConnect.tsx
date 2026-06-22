import { useEffect, useState } from 'react'
import {
  connectLinkedInCookie,
  disconnectLinkedIn,
  getLinkedInSession,
  startLinkedInInteractiveLogin,
  type LinkedInSessionInfo,
} from '../api'

interface LinkedInConnectProps {
  onSessionChange: (session: LinkedInSessionInfo) => void
}

export function LinkedInConnect({ onSessionChange }: LinkedInConnectProps) {
  const [session, setSession] = useState<LinkedInSessionInfo | null>(null)
  const [liAt, setLiAt] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [showCookieHelp, setShowCookieHelp] = useState(false)

  const refresh = async () => {
    try {
      const next = await getLinkedInSession()
      setSession(next)
      onSessionChange(next)
      return next
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao verificar sessão')
      return null
    }
  }

  useEffect(() => {
    void refresh()
  }, [])

  useEffect(() => {
    if (!session?.loginInProgress) return

    const interval = window.setInterval(() => {
      void refresh()
    }, 2000)

    return () => window.clearInterval(interval)
  }, [session?.loginInProgress])

  const handleCookieConnect = async () => {
    setLoading(true)
    setError(null)
    try {
      await connectLinkedInCookie(liAt)
      setLiAt('')
      await refresh()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha na conexão')
    } finally {
      setLoading(false)
    }
  }

  const handleInteractiveConnect = async () => {
    setLoading(true)
    setError(null)
    try {
      await startLinkedInInteractiveLogin()
      await refresh()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao iniciar login')
    } finally {
      setLoading(false)
    }
  }

  const handleDisconnect = async () => {
    setLoading(true)
    setError(null)
    try {
      await disconnectLinkedIn()
      await refresh()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao desconectar')
    } finally {
      setLoading(false)
    }
  }

  if (!session) {
    return (
      <section className="card linkedin-connect">
        <p>Verificando sessão LinkedIn…</p>
      </section>
    )
  }

  if (session.connected) {
    return (
      <section className="card linkedin-connect">
        <div className="linkedin-connect__status">
          <span className="linkedin-connect__badge linkedin-connect__badge--ok">Conectado</span>
          <p>
            Sua sessão LinkedIn está ativa. O AvaliadIN usa esse login para capturar o perfil pela URL
            — sem script no Console.
          </p>
        </div>
        <button
          type="button"
          className="btn btn--ghost btn--sm"
          onClick={handleDisconnect}
          disabled={loading}
        >
          Desconectar
        </button>
        {error && <div className="alert alert--error">{error}</div>}
      </section>
    )
  }

  return (
    <section className="card linkedin-connect">
      <h2>Conectar LinkedIn</h2>
      <p>
        Conecte uma vez. O sistema reutiliza sua sessão para ler o perfil — como se você estivesse
        logado no navegador, mas sem colar script manualmente.
      </p>

      {session.loginInProgress && (
        <div className="alert alert--info">
          Aguardando login na janela do navegador… Conclua o acesso no LinkedIn e esta tela atualiza
          sozinha.
        </div>
      )}

      {session.interactiveLoginAvailable && (
        <div className="linkedin-connect__actions">
          <button
            type="button"
            className="btn btn--primary"
            onClick={handleInteractiveConnect}
            disabled={loading || session.loginInProgress}
          >
            Abrir login do LinkedIn
          </button>
          <p className="linkedin-connect__hint">
            Uma janela do Chrome/Edge abrirá no computador onde a API está rodando.
          </p>
        </div>
      )}

      <div className="linkedin-connect__cookie">
        <label>
          Cookie de sessão (li_at)
          <input
            type="password"
            value={liAt}
            onChange={(e) => setLiAt(e.target.value)}
            placeholder="Cole o valor do cookie li_at"
            disabled={loading}
          />
        </label>
        <div className="linkedin-connect__cookie-actions">
          <button
            type="button"
            className="btn btn--primary"
            onClick={handleCookieConnect}
            disabled={loading || !liAt.trim()}
          >
            Conectar com sessão
          </button>
          <button
            type="button"
            className="btn btn--ghost btn--sm"
            onClick={() => setShowCookieHelp((v) => !v)}
          >
            {showCookieHelp ? 'Ocultar ajuda' : 'Como obter o cookie?'}
          </button>
        </div>
      </div>

      {showCookieHelp && (
        <ol className="steps linkedin-connect__steps">
          <li>
            <span>1</span> Abra <a href="https://www.linkedin.com" target="_blank" rel="noreferrer">linkedin.com</a>{' '}
            e faça login normalmente
          </li>
          <li>
            <span>2</span> Pressione <strong>F12</strong> → aba <strong>Application</strong> (ou
            Armazenamento)
          </li>
          <li>
            <span>3</span> Cookies → <code>https://www.linkedin.com</code> → copie o valor de{' '}
            <code>li_at</code>
          </li>
          <li>
            <span>4</span> Cole acima e clique em <strong>Conectar com sessão</strong>
          </li>
        </ol>
      )}

      <p className="linkedin-connect__privacy">
        O cookie fica apenas no servidor do AvaliadIN (volume local). Não enviamos para serviços
        externos.
      </p>

      {error && <div className="alert alert--error">{error}</div>}
    </section>
  )
}
