window.__AVALIADIN_EXTENSION__ = true

window.addEventListener('avaliadin-request-capture', function (event) {
  var detail = event.detail || {}
  chrome.runtime.sendMessage({
    type: 'START_CAPTURE',
    url: detail.url,
    apiBase: detail.apiBase || 'http://localhost:5080',
    requestId: detail.requestId || String(Date.now()),
  })
})

chrome.runtime.onMessage.addListener(function (message) {
  if (message.type === 'EVALUATION_RESULT') {
    window.dispatchEvent(new CustomEvent('avaliadin-evaluation', { detail: message.data }))
  }
  if (message.type === 'CAPTURE_ERROR') {
    window.dispatchEvent(new CustomEvent('avaliadin-capture-error', { detail: message.error }))
  }
})
