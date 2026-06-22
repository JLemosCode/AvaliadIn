const pendingRequests = new Map()

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.type === 'START_CAPTURE') {
    handleStartCapture(message).then(sendResponse).catch((err) => sendResponse({ error: err.message }))
    return true
  }

  if (message.type === 'PROFILE_CAPTURED') {
    handleProfileCaptured(message).then(sendResponse).catch((err) => sendResponse({ error: err.message }))
    return true
  }
})

async function handleStartCapture(message) {
  const { url, apiBase, requestId } = message
  pendingRequests.set(requestId, { apiBase })

  await chrome.storage.session.set({
    pendingCapture: { url, requestId },
  })

  await chrome.tabs.create({ url, active: true })
  return { started: true, requestId }
}

async function notifyAvaliadINTabs(payload) {
  const tabs = await chrome.tabs.query({
    url: [
      'http://localhost:3000/*',
      'http://localhost:5173/*',
      'http://127.0.0.1:3000/*',
      'http://127.0.0.1:5173/*',
    ],
  })

  for (const tab of tabs) {
    if (tab.id) {
      chrome.tabs.sendMessage(tab.id, payload).catch(() => {})
    }
  }
}

async function handleProfileCaptured(message) {
  const { profile, sourceUrl, requestId } = message
  const pending = pendingRequests.get(requestId)
  if (!pending) return { ok: false }

  pendingRequests.delete(requestId)

  try {
    const apiBase = pending.apiBase || 'http://localhost:5080'
    const response = await fetch(`${apiBase}/api/v1/evaluate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(profile),
    })

    if (!response.ok) {
      const body = await response.json().catch(() => ({}))
      throw new Error(body.error || `Erro ${response.status}`)
    }

    const evaluation = await response.json()
    const result = {
      sourceUrl,
      profile,
      evaluation,
      quality: 'good',
      importWarnings: [],
      detectedFields: ['headline', 'about', 'experiences', 'skills'].filter((f) => {
        if (f === 'headline') return !!profile.headline
        if (f === 'about') return !!profile.about
        if (f === 'experiences') return profile.experiences?.some((e) => e.title)
        if (f === 'skills') return profile.skills?.length
        return false
      }),
    }

    await notifyAvaliadINTabs({ type: 'EVALUATION_RESULT', data: result })
    return { ok: true }
  } catch (err) {
    const errorMessage = err instanceof Error ? err.message : 'Falha na avaliação'
    await notifyAvaliadINTabs({ type: 'CAPTURE_ERROR', error: errorMessage })
    throw err
  }
}
