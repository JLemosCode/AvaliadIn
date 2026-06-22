chrome.storage.session.get(['pendingCapture'], function (data) {
  var pending = data.pendingCapture
  if (!pending) return

  var current = window.location.href.split('?')[0].replace(/\/$/, '')
  var target = pending.url.split('?')[0].replace(/\/$/, '')
  if (!current.startsWith(target) && !target.startsWith(current)) return

  function captureAndSend() {
    try {
      var profile = extractLinkedInProfile()
      if (!profile.headline && !profile.about && profile.experiences.every(function (e) { return !e.title })) {
        return false
      }
      chrome.runtime.sendMessage({
        type: 'PROFILE_CAPTURED',
        profile: profile,
        sourceUrl: window.location.href.split('?')[0],
        requestId: pending.requestId,
      })
      chrome.storage.session.remove(['pendingCapture'])
      return true
    } catch (e) {
      console.error('[AvaliadIN]', e)
      return false
    }
  }

  setTimeout(function () {
    if (captureAndSend()) return
    window.scrollTo(0, document.body.scrollHeight / 2)
    setTimeout(function () {
      window.scrollTo(0, document.body.scrollHeight)
      setTimeout(captureAndSend, 1500)
    }, 1500)
  }, 2000)
})
