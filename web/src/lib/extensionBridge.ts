import type { LinkedInEvaluationResult } from '../types'

declare global {
  interface Window {
    __AVALIADIN_EXTENSION__?: boolean
  }
}

const CAPTURE_TIMEOUT_MS = 90_000

export function isExtensionInstalled(): boolean {
  return window.__AVALIADIN_EXTENSION__ === true
}

/** Aguarda a extensão injetar o bridge (content script pode carregar após o React) */
export function waitForExtension(maxMs = 2000): Promise<boolean> {
  if (isExtensionInstalled()) return Promise.resolve(true)

  return new Promise((resolve) => {
    const started = Date.now()
    const tick = () => {
      if (isExtensionInstalled()) {
        resolve(true)
        return
      }
      if (Date.now() - started >= maxMs) {
        resolve(false)
        return
      }
      window.setTimeout(tick, 100)
    }
    tick()
  })
}

export function requestCaptureViaExtension(
  url: string,
  apiBase: string,
): Promise<LinkedInEvaluationResult> {
  return new Promise((resolve, reject) => {
    const requestId = String(Date.now())
    let settled = false

    const timeout = window.setTimeout(() => {
      if (settled) return
      settled = true
      cleanup()
      reject(new Error('Tempo esgotado. Verifique se está logado no LinkedIn e se a extensão está ativa.'))
    }, CAPTURE_TIMEOUT_MS)

    const onResult = (event: Event) => {
      if (settled) return
      settled = true
      cleanup()
      resolve((event as CustomEvent<LinkedInEvaluationResult>).detail)
    }

    const onError = (event: Event) => {
      if (settled) return
      settled = true
      cleanup()
      reject(new Error(String((event as CustomEvent<string>).detail)))
    }

    function cleanup() {
      window.clearTimeout(timeout)
      window.removeEventListener('avaliadin-evaluation', onResult)
      window.removeEventListener('avaliadin-capture-error', onError)
    }

    window.addEventListener('avaliadin-evaluation', onResult)
    window.addEventListener('avaliadin-capture-error', onError)

    window.dispatchEvent(
      new CustomEvent('avaliadin-request-capture', {
        detail: { url, apiBase, requestId },
      }),
    )
  })
}
