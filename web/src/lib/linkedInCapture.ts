import { useCallback, useEffect, useState } from 'react'
import type { ProfileEvaluationRequest } from '../types'

export interface CapturedPayload {
  profile: ProfileEvaluationRequest
  sourceUrl: string
}

function isLinkedInOrigin(origin: string): boolean {
  return origin.includes('linkedin.com')
}

function isCaptureMessage(data: unknown): data is {
  type: 'avaliadin-capture'
  profile: ProfileEvaluationRequest
  sourceUrl: string
} {
  return (
    typeof data === 'object' &&
    data !== null &&
    (data as { type?: string }).type === 'avaliadin-capture' &&
    typeof (data as { sourceUrl?: string }).sourceUrl === 'string' &&
    typeof (data as { profile?: unknown }).profile === 'object'
  )
}

export function useLinkedInCapture(onCapture: (payload: CapturedPayload) => void) {
  const handleMessage = useCallback(
    (event: MessageEvent) => {
      if (!isLinkedInOrigin(event.origin)) return
      if (!isCaptureMessage(event.data)) return
      onCapture({ profile: event.data.profile, sourceUrl: event.data.sourceUrl })
    },
    [onCapture],
  )

  useEffect(() => {
    window.addEventListener('message', handleMessage)

    let channel: BroadcastChannel | null = null
    try {
      channel = new BroadcastChannel('avaliadin-capture')
      channel.onmessage = (event: MessageEvent) => {
        if (!isCaptureMessage(event.data)) return
        onCapture({ profile: event.data.profile, sourceUrl: event.data.sourceUrl })
      }
    } catch {
      // BroadcastChannel indisponível em navegadores antigos
    }

    return () => {
      window.removeEventListener('message', handleMessage)
      channel?.close()
    }
  }, [handleMessage, onCapture])
}

export function parseCapturedJson(raw: string): CapturedPayload {
  const data = JSON.parse(raw) as CapturedPayload
  if (!data.profile || !data.sourceUrl) {
    throw new Error('JSON inválido. Use o script de captura do AvaliadIN.')
  }
  return data
}

export async function loadCaptureSnippet(): Promise<string> {
  const response = await fetch('/capture-snippet.js')
  if (!response.ok) throw new Error('Não foi possível carregar o script de captura.')
  return response.text()
}

export function useCaptureSnippet() {
  const [snippet, setSnippet] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadCaptureSnippet()
      .then(setSnippet)
      .catch(() => setSnippet(null))
      .finally(() => setLoading(false))
  }, [])

  return { snippet, loading }
}
