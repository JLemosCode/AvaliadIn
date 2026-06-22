/* global extractLinkedInProfile */
function extractLinkedInProfile() {
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
      section
        .querySelectorAll('li.artdeco-list__item, div.pvs-list__paged-list-item, ul.pvs-list li')
        .forEach(function (item) {
          var titleEl = item.querySelector(
            '.t-bold span[aria-hidden="true"], .mr1 span[aria-hidden="true"], h3 span[aria-hidden="true"]',
          )
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

  return {
    headline: headline,
    about: about,
    experiences: experiences.length ? experiences : [{ title: '', company: '', description: '' }],
    skills: skills.slice(0, 25),
    pinnedSkills: skills.slice(0, 3),
    targetRole: headline || 'Profissional',
    openToWork: {
      enabled: false,
      recruitersOnly: true,
      targetTitles: [],
      remote: false,
      contract: false,
      fullTime: true,
    },
    activity: {
      postsLast90Days: 0,
      commentsPerWeek: 0,
      invitesPerWeek: 0,
      profileViewsPerDay: 0,
      searchesPerWeek: 0,
      respondsToInMail: false,
      creatorMode: false,
    },
    completeness: {
      hasPhoto: !!document.querySelector('.pv-top-card-profile-picture img, img.pv-top-card-profile-picture__image'),
      hasBanner: !!document.querySelector('.profile-background-image'),
      hasFeatured: !!document.getElementById('featured'),
      recommendations: 0,
      endorsementsTopSkills: 0,
    },
  }
}

if (typeof window !== 'undefined') {
  window.extractLinkedInProfile = extractLinkedInProfile
}
