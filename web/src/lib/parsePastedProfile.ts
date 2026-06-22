import { createEmptyRequest } from '../sampleData'
import type { ExperienceInput, ProfileEvaluationRequest } from '../types'

const SECTION_MARKERS =
  /^(about|sobre|experience|experiência|experiencia|skills|competências|competencias|education|formação|formacao|licenses|licenças|licencas|projects|projetos|recommendations|recomendações)$/i

function splitSections(text: string): Map<string, string> {
  const sections = new Map<string, string>()
  const lines = text.replace(/\r\n/g, '\n').split('\n')
  let currentKey = '_header'
  let buffer: string[] = []

  const flush = () => {
    if (buffer.length > 0) sections.set(currentKey, buffer.join('\n').trim())
    buffer = []
  }

  for (const raw of lines) {
    const line = raw.trim()
    if (SECTION_MARKERS.test(line)) {
      flush()
      currentKey = line.toLowerCase()
      continue
    }
    buffer.push(raw)
  }
  flush()
  return sections
}

function parseExperiences(block: string): ExperienceInput[] {
  const chunks = block
    .split(/\n{2,}/)
    .map((c) => c.trim())
    .filter(Boolean)

  const experiences: ExperienceInput[] = []

  for (const chunk of chunks) {
    const lines = chunk.split('\n').map((l) => l.trim()).filter(Boolean)
    if (lines.length === 0) continue

    const title = lines[0]
    if (/^(experience|experiência|experiencia)$/i.test(title)) continue

    const company =
      lines.find((l, i) => i > 0 && (l.includes(' · ') || l.includes(' | ')))?.split(/ · | \| /)[0] ??
      lines[1] ??
      ''

    const description = lines
      .slice(1)
      .filter((l) => l !== company && !/^\d/.test(l) && !/^(jan|fev|mar|abr|mai|jun|jul|ago|set|out|nov|dez|present|atual)/i.test(l))
      .join('\n')

    if (title.length > 2) {
      experiences.push({ title, company, description })
    }
  }

  return experiences.length > 0 ? experiences.slice(0, 8) : [{ title: '', company: '', description: '' }]
}

function parseSkills(block: string): string[] {
  return block
    .split(/[\n,·|]/)
    .map((s) => s.trim())
    .filter((s) => s.length > 1 && s.length < 50 && !SECTION_MARKERS.test(s))
    .filter((s, i, arr) => arr.indexOf(s) === i)
    .slice(0, 25)
}

function guessHeadline(header: string): string {
  const lines = header.split('\n').map((l) => l.trim()).filter(Boolean)
  if (lines.length === 0) return ''

  let idx = 0
  if (lines[0].split(/\s+/).length <= 4 && !lines[0].includes('|') && lines[0].length < 40) {
    idx = 1
  }

  const candidate = lines[idx] ?? lines[0] ?? ''
  if (SECTION_MARKERS.test(candidate)) return ''
  if (/^\d/.test(candidate)) return ''
  if (/^(he\/him|she\/her|they|ele\/dele|ela\/dela)/i.test(candidate)) {
    return lines[idx + 1] ?? ''
  }
  return candidate
}

export function parsePastedLinkedInText(text: string): ProfileEvaluationRequest {
  const base = createEmptyRequest()
  if (!text.trim()) return base

  const sections = splitSections(text)
  const header = sections.get('_header') ?? ''
  const about =
    sections.get('about') ??
    sections.get('sobre') ??
    ''

  const expBlock =
    sections.get('experience') ??
    sections.get('experiência') ??
    sections.get('experiencia') ??
    ''

  const skillsBlock =
    sections.get('skills') ??
    sections.get('competências') ??
    sections.get('competencias') ??
    ''

  const headline = guessHeadline(header)
  const experiences = expBlock ? parseExperiences(expBlock) : base.experiences
  const skills = skillsBlock ? parseSkills(skillsBlock) : base.skills

  return {
    ...base,
    headline,
    about: about.trim(),
    experiences,
    skills,
    pinnedSkills: skills.slice(0, 3),
    targetRole: headline || base.targetRole,
  }
}
