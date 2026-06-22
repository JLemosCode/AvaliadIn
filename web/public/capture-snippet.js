/**
 * Script standalone — cole no Console (F12) estando na página do seu perfil LinkedIn.
 * Envia os dados para o AvaliadIN aberto em outra aba.
 */
(function avaliadINCapture() {
  function getText(sel) {
    var el = document.querySelector(sel)
    return el ? el.textContent.trim() : ''
  }

  function sectionText(id) {
    var anchor = document.getElementById(id)
    if (!anchor) return ''
    var section = anchor.closest('section')
    if (!section) return ''
    var lines = (section.innerText || '').split('\n').map(function (l) {
      return l.trim()
    }).filter(Boolean)
    return lines.slice(1).join('\n').trim()
  }

  var experiences = []
  var expSection = document.getElementById('experience')
  if (expSection) {
    var section = expSection.closest('section')
    if (section) {
      section.querySelectorAll('li.artdeco-list__item, div.pvs-list__paged-list-item, ul.pvs-list li').forEach(function (item) {
        var titleEl = item.querySelector('.t-bold span[aria-hidden="true"], .mr1 span[aria-hidden="true"], h3 span[aria-hidden="true"]')
        var companyEl = item.querySelector('.t-14.t-normal span[aria-hidden="true"]')
        var descEl = item.querySelector('.inline-show-more-text, .pv-shared-text-with-see-more')
        if (titleEl) {
          experiences.push({
            title: titleEl.textContent.trim(),
            company: companyEl ? companyEl.textContent.trim() : '',
            description: descEl ? descEl.textContent.trim() : '',
          })
        }
      })
    }
  }

  var skills = []
  var skillsSection = document.getElementById('skills')
  if (skillsSection) {
    var skSection = skillsSection.closest('section')
    if (skSection) {
      skSection.querySelectorAll('.t-bold span[aria-hidden="true"]').forEach(function (el) {
        var s = el.textContent.trim()
        if (s && s.length < 50 && skills.indexOf(s) === -1) skills.push(s)
      })
    }
  }

  var headline =
    getText('h1.text-heading-xlarge') ||
    getText('.top-card-layout__headline') ||
    getText('h1.inline.t-24') ||
    ''
  var about = sectionText('about')

  if (!headline && !about && experiences.length === 0) {
    alert('Nenhum dado encontrado. Abra a página completa do seu perfil (/in/seu-nome) estando logado.')
    return
  }

  var profile = {
    headline: headline,
    about: about,
    experiences: experiences.length ? experiences : [{ title: '', company: '', description: '' }],
    skills: skills.slice(0, 25),
    pinnedSkills: skills.slice(0, 3),
    targetRole: headline || 'Profissional',
  }
  var sourceUrl = window.location.href.split('?')[0]
  var message = { type: 'avaliadin-capture', profile: profile, sourceUrl: sourceUrl }

  var sent = false

  if (window.opener && !window.opener.closed) {
    try {
      window.opener.postMessage(message, '*')
      sent = true
    } catch (e) {}
  }

  try {
    var channel = new BroadcastChannel('avaliadin-capture')
    channel.postMessage(message)
    channel.close()
    sent = true
  } catch (e) {}

  if (sent) {
    alert('Perfil enviado ao AvaliadIN! Volte para a aba do AvaliadIN.')
    return
  }

  var payload = JSON.stringify({ profile: profile, sourceUrl: sourceUrl }, null, 2)
  if (navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(payload).then(function () {
      alert('Perfil copiado! Cole no AvaliadIN na área "Colar dados capturados".')
    })
  } else {
    prompt('Copie este JSON e cole no AvaliadIN:', payload)
  }
})()
