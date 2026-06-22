using System.Text;
using System.Text.RegularExpressions;
using AvaliadIN.Core.Models;
using UglyToad.PdfPig;

namespace AvaliadIN.Api.LinkedIn;

public static class LinkedInPdfProfileParser
{
    private static readonly Regex LinkedInUrlRegex =
        new(@"https?://(?:www\.)?linkedin\.com/in/[\w%-]+|(?:www\.)?linkedin\.com/in/[\w%-]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LinkedInSlugRegex =
        new(@"linkedin\.com/in/([\w%-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DateLineRegex = new(
        @"(?i)(?:\b(?:jan(?:\.|eiro)?|fev(?:\.|ereiro)?|feb(?:ruary|\.)?|mar(?:\.|ço|co)?|abr(?:\.|il)?|apr(?:il|\.)?|mai(?:\.|o)?|may(?:\.)?|jun(?:\.|ho)?|jul(?:\.|ho)?|ago(?:\.|sto)?|aug(?:ust|\.)?|set(?:\.|embro)?|sep(?:t(?:ember|\.|embro)?)?|out(?:\.|ubro)?|oct(?:ober|\.)?|nov(?:\.|embro)?|dez(?:\.|embro)?|dec(?:ember|\.)?)\s*(?:de\s+)?\d{4}|\d{4}\s*[-–—]\s*(?:\d{4}|present(?:e)?|atual|o\s+momento|now|atualmente))(?:\s*[-–—]\s*(?:\b(?:jan(?:\.|eiro)?|fev(?:\.|ereiro)?|feb(?:ruary|\.)?|mar(?:\.|ço|co)?|abr(?:\.|il)?|apr(?:il|\.)?|mai(?:\.|o)?|may(?:\.)?|jun(?:\.|ho)?|jul(?:\.|ho)?|ago(?:\.|sto)?|aug(?:ust|\.)?|set(?:\.|embro)?|sep(?:t(?:ember|\.|embro)?)?|out(?:\.|ubro)?|oct(?:ober|\.)?|nov(?:\.|embro)?|dez(?:\.|embro)?|dec(?:ember|\.)?)\s*(?:de\s+)?\d{4}|\d{4}|present(?:e)?|atual|o\s+momento|now|atualmente))?(?:\s*\([^)]+\))?",
        RegexOptions.Compiled);

    private static readonly Regex LocationLineRegex = new(
        @"\b(?:região|region|area|área|metropolitan|brasil|brazil)\b|[,\s]+[A-Z]{2}\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static LinkedInImportResult Parse(Stream pdfStream)
    {
        var rawText = ExtractText(pdfStream);
        if (string.IsNullOrWhiteSpace(rawText))
            throw new InvalidOperationException("PDF vazio ou ilegível. Exporte novamente pelo LinkedIn.");

        var lines = NormalizeLines(rawText);
        var warnings = new List<string>();
        var detected = new List<string>();

        var sourceUrl = ExtractLinkedInUrl(rawText, lines);
        if (sourceUrl.Contains("/perfil", StringComparison.Ordinal))
            warnings.Add("URL do perfil reconstruída a partir do PDF — confira no formulário.");

        var sectionStarts = FindSectionStarts(lines);

        var about = ExtractAbout(lines, sectionStarts);
        if (string.IsNullOrWhiteSpace(about))
            warnings.Add("Seção Sobre/Resumo não encontrada no PDF (comum quando o campo está vazio no LinkedIn).");

        var skills = ExtractSkills(lines, sectionStarts);
        var (headline, name) = ExtractNameAndHeadline(lines, sectionStarts, skills);

        if (string.IsNullOrWhiteSpace(headline))
            warnings.Add("Headline não identificada — confira no formulário.");
        else
            detected.Add("headline");

        if (!string.IsNullOrWhiteSpace(about))
            detected.Add("about");

        if (skills.Count > 0)
            detected.Add("skills");
        else
            warnings.Add("Skills não encontradas no PDF.");

        var experiences = ExtractExperiences(lines, sectionStarts);
        if (experiences.Count > 0 && experiences.Any(e =>
                !string.IsNullOrWhiteSpace(e.Title) || !string.IsNullOrWhiteSpace(e.Company)))
        {
            detected.Add("experiences");
            warnings.Add(
                $"{experiences.Count} experiência(s) detectada(s) no PDF — confira cargo, empresa e descrições.");
        }
        else
            warnings.Add("Experiências não encontradas ou incompletas no PDF.");

        var profile = new ProfileEvaluationRequest
        {
            Headline = headline,
            About = about,
            Experiences = experiences.Count > 0
                ? experiences
                : [new ExperienceInput { Title = "", Company = "", Description = "" }],
            Skills = skills,
            PinnedSkills = skills.Take(3).ToList(),
            TargetRole = !string.IsNullOrWhiteSpace(headline) ? headline : name
        };

        var quality = detected.Count >= 3 ? "good" : detected.Count >= 2 ? "partial" : "minimal";

        return new LinkedInImportResult
        {
            SourceUrl = sourceUrl,
            Profile = profile,
            Quality = quality,
            Warnings = warnings,
            DetectedFields = detected
        };
    }

    private static string ExtractText(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var sb = new StringBuilder();
        foreach (var page in document.GetPages())
            sb.AppendLine(page.Text);

        return sb.ToString();
    }

    private static List<string> NormalizeLines(string rawText)
    {
        var text = rawText.Replace('\r', '\n').Replace('\u00a0', ' ');

        // PdfPig costuma colar seções na mesma linha — inserir quebras artificiais
        text = InsertLinkedInPdfBreaks(text);

        return text
            .Split('\n')
            .Select(l => Regex.Replace(l.Trim(), @"\s+", " "))
            .Where(l => l.Length > 0)
            .Where(l => !l.Equals("Page 1 of 1", StringComparison.OrdinalIgnoreCase))
            .Where(l => !Regex.IsMatch(l, @"^Page\s+\d+\s+of\s+\d+$", RegexOptions.IgnoreCase))
            .ToList();
    }

    private static string InsertLinkedInPdfBreaks(string text)
    {
        var markers = new[]
        {
            "Informações de contato", "Informacoes de contato",
            "Contato", "Contact",
            "Principais competências", "Principais competencias",
            "Top Skills", "Skills", "Competências", "Competencias",
            "Resumo", "Summary", "Sobre", "About",
            "Experiências", "Experiencias", "Experiência", "Experiencia", "Experience", "Work Experience",
            "Formação acadêmica", "Formacao academica", "Formação", "Formacao", "Education",
            "Certificações", "Certificacoes", "Licenses", "Licenças",
            "Languages", "Idiomas"
        };

        foreach (var marker in markers.OrderByDescending(m => m.Length))
        {
            text = Regex.Replace(
                text,
                $@"(?<=\S)({Regex.Escape(marker)})(?=\S|\s)",
                $"\n$1\n",
                RegexOptions.IgnoreCase);
        }

        text = Regex.Replace(text, @"(Contato|Contact)(www\.)", "$1\n$2", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(\(LinkedIn\))", "\n$1\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(\))(\s*)(Formação|Formacao|Education)", "$1\n$3", RegexOptions.IgnoreCase);

        // Separa palavras coladas: "organizaçãoGeovanna", "CRMProspecção", "Managerna Times"
        text = Regex.Replace(text, @"([a-zà-ÿ])([A-ZÁÉÍÓÚÃÕ])", "$1\n$2");
        text = Regex.Replace(text, @"([A-Z]{2,})([A-Z][a-zà-ÿ])", "$1\n$2");
        text = Regex.Replace(
            text,
            @"(Manager|Consultant|Engineer|Director|Lead|Recruiter|Designer|Developer|Coordinator|Specialist|Architect|Analyst)(na|em)\s+",
            "$1\n$2 ",
            RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(\))(\s*)([A-Z])", "$1\n$3");

        // Separa cargo colado à data: "Headhuntermarço de 2025", "Stackjan. de 2020"
        text = Regex.Replace(
            text,
            @"(?<=[a-zà-ÿ])(?=(?:jan(?:\.|eiro)?|fev(?:\.|ereiro)?|feb(?:ruary|\.)?|mar(?:\.|ço|co)?|abr(?:\.|il)?|apr(?:il|\.)?|mai(?:\.|o)?|may(?:\.)?|jun(?:\.|ho)?|jul(?:\.|ho)?|ago(?:\.|sto)?|aug(?:ust|\.)?|set(?:\.|embro)?|sep(?:t(?:ember|\.|embro)?)?|out(?:\.|ubro)?|oct(?:ober|\.)?|nov(?:\.|embro)?|dez(?:\.|embro)?|dec(?:ember|\.)?)\s*(?:de\s+)?\d{4})",
            "\n",
            RegexOptions.IgnoreCase);

        return text;
    }

    private static string ExtractLinkedInUrl(string rawText, List<string> lines)
    {
        var collapsed = Regex.Replace(rawText.Replace('\r', ' '), @"\s+", " ");
        var match = LinkedInUrlRegex.Match(collapsed);
        if (match.Success)
            return NormalizeLinkedInUrl(match.Value);

        var urlParts = new StringBuilder();
        foreach (var line in lines)
        {
            if (line.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase))
                urlParts.Append(line.Replace("(LinkedIn)", "", StringComparison.OrdinalIgnoreCase).Trim());
        }

        if (urlParts.Length == 0)
            return "https://www.linkedin.com/in/perfil";

        var slugMatch = LinkedInSlugRegex.Match(urlParts.ToString().Replace(" ", ""));
        if (!slugMatch.Success)
            return "https://www.linkedin.com/in/perfil";

        var slug = slugMatch.Groups[1].Value.TrimEnd('-');
        return $"https://www.linkedin.com/in/{slug}";
    }

    private static string NormalizeLinkedInUrl(string url)
    {
        var cleaned = url.Trim().Replace(" ", "");
        if (!cleaned.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            cleaned = "https://" + cleaned;

        return cleaned.Split('?')[0];
    }

    private sealed record SectionStarts(
        int? Contact,
        int? Skills,
        int? Summary,
        int? Experience,
        int? Education);

    private static SectionStarts FindSectionStarts(List<string> lines)
    {
        int? contact = null, skills = null, summary = null, experience = null, education = null;

        for (var i = 0; i < lines.Count; i++)
        {
            var key = MatchSectionKey(lines[i]);
            switch (key)
            {
                case "_contact": contact ??= i; break;
                case "skills": skills ??= i; break;
                case "summary": summary ??= i; break;
                case "experience": experience ??= i; break;
                case "education": education ??= i; break;
            }
        }

        return new SectionStarts(contact, skills, summary, experience, education);
    }

    private static string ExtractAbout(List<string> lines, SectionStarts sections)
    {
        if (sections.Summary is not int summaryIdx)
            return string.Empty;

        var end = NextSectionIndex(lines, summaryIdx, sections);
        return string.Join("\n", lines.Skip(summaryIdx + 1).Take(end - summaryIdx - 1)).Trim();
    }

    private static List<string> ExtractSkills(List<string> lines, SectionStarts sections)
    {
        if (sections.Skills is not int skillsIdx)
            return [];

        var end = sections.Experience ?? sections.Summary ?? lines.Count;
        var block = lines.Skip(skillsIdx + 1).Take(end - skillsIdx - 1).ToList();
        var nameIdx = FindNameIndex(block);

        var skillLines = nameIdx > 0 ? block.Take(nameIdx) : block;
        return skillLines
            .Where(s => !LooksLikePersonName(s))
            .Where(s => !LooksLikeLocation(s))
            .Where(s => !LooksLikeEmploymentSnippet(s))
            .Where(s => !IsNoiseLine(s))
            .Where(s => s.Length > 2 && s.Length < 60)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();
    }

    private static (string Headline, string Name) ExtractNameAndHeadline(
        List<string> lines,
        SectionStarts sections,
        List<string> knownSkills)
    {
        if (sections.Experience is not int expIdx)
            return (string.Empty, string.Empty);

        var start = (sections.Skills ?? sections.Summary ?? sections.Contact ?? -1) + 1;
        if (start < 0) start = 0;

        var block = lines.Skip(start).Take(expIdx - start)
            .Where(l => !IsNoiseLine(l))
            .Where(l => !knownSkills.Contains(l, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (block.Count == 0)
            return (string.Empty, string.Empty);

        var nameIdx = FindNameIndex(block);
        if (nameIdx < 0)
            return (GuessHeadlineFromBlock(block), string.Empty);

        var name = block[nameIdx];
        var afterName = block.Skip(nameIdx + 1)
            .Where(l => !LooksLikeLocation(l))
            .Where(l => !LooksLikeEmploymentSnippet(l))
            .ToList();

        if (afterName.Count == 0)
            return (string.Empty, name);

        var headline = string.Join(" ", afterName).Trim();
        return (headline, name);
    }

    private static int FindNameIndex(IReadOnlyList<string> block)
    {
        for (var i = 0; i < block.Count; i++)
        {
            if (LooksLikePersonName(block[i]))
                return i;
        }

        return -1;
    }

    private static bool LooksLikePersonName(string line)
    {
        if (line.Contains('|', StringComparison.Ordinal) ||
            line.Contains('@', StringComparison.Ordinal) ||
            line.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase) ||
            DateLineRegex.IsMatch(line) ||
            IsSectionMarkerLine(line))
            return false;

        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length is < 2 or > 5)
            return false;

        if (line.Length > 55)
            return false;

        return words.All(w => w.Length > 0 && char.IsUpper(w[0]));
    }

    private static bool LooksLikeLocation(string line) =>
        LocationLineRegex.IsMatch(line) ||
        line.Contains("Região", StringComparison.OrdinalIgnoreCase) ||
        line.Contains(" e Região", StringComparison.OrdinalIgnoreCase) ||
        Regex.IsMatch(line, @"^[A-Za-zÀ-ÿ\s.'-]+,\s*[A-Z]{2}$");

    private static bool LooksLikeEmploymentSnippet(string line) =>
        Regex.IsMatch(line, @"^(na|em|no|nos|at|in)\s+", RegexOptions.IgnoreCase);

    private static string GuessHeadlineFromBlock(IReadOnlyList<string> block)
    {
        var withPipe = block.FirstOrDefault(l => l.Contains('|', StringComparison.Ordinal));
        if (withPipe is not null)
            return withPipe;

        return block.FirstOrDefault(l => l.Length > 25) ?? block.FirstOrDefault() ?? string.Empty;
    }

    private static List<ExperienceInput> ExtractExperiences(List<string> lines, SectionStarts sections)
    {
        List<string> block;

        if (sections.Experience is int expIdx)
        {
            var end = FirstSectionAfter(lines, expIdx, sections);
            block = SplitLinesWithEmbeddedDates(lines.Skip(expIdx + 1).Take(end - expIdx - 1));
        }
        else
        {
            // Fallback: PDF sem cabeçalho "Experiência" explícito — procura bloco com datas após o nome
            var start = (sections.Skills ?? sections.Summary ?? sections.Contact ?? 0) + 1;
            var end = sections.Education ?? lines.Count;
            block = SplitLinesWithEmbeddedDates(lines.Skip(start).Take(end - start));
        }

        return ParseExperiences(block);
    }

    private static int FirstSectionAfter(List<string> lines, int afterIdx, SectionStarts sections)
    {
        var candidates = new[]
            {
                sections.Education,
                sections.Summary,
                sections.Skills,
                sections.Contact
            }
            .Where(i => i is int idx && idx > afterIdx)
            .Select(i => i!.Value)
            .OrderBy(i => i)
            .ToList();

        return candidates.FirstOrDefault(lines.Count);
    }

    private static int NextSectionIndex(List<string> lines, int currentIdx, SectionStarts sections)
    {
        var candidates = new[]
        {
            sections.Contact, sections.Skills, sections.Summary, sections.Experience, sections.Education
        }
        .Where(i => i is int idx && idx > currentIdx)
        .Select(i => i!.Value)
        .OrderBy(i => i)
        .ToList();

        return candidates.FirstOrDefault(lines.Count);
    }

    private static string? MatchSectionKey(string line)
    {
        var normalized = Regex.Replace(line.Trim().ToLowerInvariant(), @"\s+", " ");

        if (normalized is "contact" or "contato" or "info"
            or "informações de contato" or "informacoes de contato")
            return "_contact";

        if (normalized is "summary" or "resumo" or "sobre" or "about"
            || normalized.StartsWith("sobre ", StringComparison.Ordinal)
            || normalized.StartsWith("about ", StringComparison.Ordinal))
            return "summary";

        if (normalized is "experience" or "experiences" or "work experience"
            or "experiência" or "experiencia" or "experiências" or "experiencias"
            || normalized.StartsWith("experiência", StringComparison.Ordinal)
            || normalized.StartsWith("experiencia", StringComparison.Ordinal)
            || normalized.StartsWith("experiências", StringComparison.Ordinal)
            || normalized.StartsWith("experiencias", StringComparison.Ordinal))
            return "experience";

        if (normalized.Contains("formação acadêmica", StringComparison.Ordinal)
            || normalized.Contains("formacao academica", StringComparison.Ordinal)
            || normalized is "education" or "educação" or "educacao"
            || normalized is "formação" or "formacao")
            return "education";

        if (normalized is "skills" or "competências" or "competencias" or "top skills"
            or "principais competências" or "principais competencias"
            || normalized.StartsWith("principais compet", StringComparison.Ordinal))
            return "skills";

        return null;
    }

    private static bool IsSectionMarkerLine(string line) => MatchSectionKey(line) is not null;

    private static bool IsNoiseLine(string line) =>
        line.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase) ||
        line.Contains("(LinkedIn)", StringComparison.OrdinalIgnoreCase) ||
        line.Equals("Contato", StringComparison.OrdinalIgnoreCase);

    private static List<string> SplitLinesWithEmbeddedDates(IEnumerable<string> lines)
    {
        var result = new List<string>();
        foreach (var line in lines)
        {
            var match = DateLineRegex.Match(line);
            if (match.Success && match.Index > 0)
            {
                var prefix = line[..match.Index].Trim();
                var datePart = line[match.Index..].Trim();
                if (!string.IsNullOrWhiteSpace(prefix))
                    result.Add(prefix);
                if (!string.IsNullOrWhiteSpace(datePart))
                    result.Add(datePart);
                continue;
            }

            result.Add(line);
        }

        return result;
    }

    private static List<ExperienceInput> ParseExperiences(IReadOnlyList<string> lines)
    {
        var cleaned = lines
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Where(l => !IsNoiseLine(l))
            .Where(l => !IsSectionMarkerLine(l))
            .ToList();

        if (cleaned.Count == 0)
            return [];

        var results = new List<ExperienceInput>();

        for (var i = 0; i < cleaned.Count; i++)
        {
            var line = cleaned[i];
            var dateMatch = DateLineRegex.Match(line);
            if (!dateMatch.Success)
                continue;

            var headerLines = new List<string>();
            if (dateMatch.Index > 0)
            {
                var prefix = line[..dateMatch.Index].Trim();
                if (!string.IsNullOrWhiteSpace(prefix))
                    headerLines.Add(prefix);
            }

            for (var j = i - 1; j >= 0 && headerLines.Count < 2; j--)
            {
                var prev = cleaned[j];
                if (DateLineRegex.IsMatch(prev))
                    break;
                if (IsBulletLine(prev))
                    break;
                if (LooksLikeLocation(prev) && headerLines.Count > 0)
                    break;
                headerLines.Insert(0, prev);
            }

            var descLines = new List<string>();
            for (var j = i + 1; j < cleaned.Count; j++)
            {
                var next = cleaned[j];
                if (DateLineRegex.IsMatch(next))
                    break;
                if (IsNextExperienceHeader(cleaned, j))
                    break;
                if (IsSectionMarkerLine(next))
                    break;
                if (LooksLikeLocation(next) && descLines.Count == 0)
                    continue;
                descLines.Add(next);
            }

            while (descLines.Count > 0 && LooksLikeLocation(descLines[^1]))
                descLines.RemoveAt(descLines.Count - 1);

            var exp = BuildExperienceFromHeader(headerLines, descLines);
            if (!string.IsNullOrWhiteSpace(exp.Title) || !string.IsNullOrWhiteSpace(exp.Company))
                results.Add(exp);
        }

        if (results.Count == 0)
            return ParseExperiencesLegacy(cleaned);

        return DeduplicateExperiences(results).Take(12).ToList();
    }

    private static bool IsNextExperienceHeader(IReadOnlyList<string> lines, int idx)
    {
        if (DateLineRegex.IsMatch(lines[idx]))
            return false;

        if (idx + 1 < lines.Count && DateLineRegex.IsMatch(lines[idx + 1]))
            return true;

        if (idx + 2 < lines.Count
            && !DateLineRegex.IsMatch(lines[idx])
            && !DateLineRegex.IsMatch(lines[idx + 1])
            && DateLineRegex.IsMatch(lines[idx + 2]))
            return true;

        return false;
    }

    private static ExperienceInput BuildExperienceFromHeader(
        IReadOnlyList<string> headerLines,
        IReadOnlyList<string> descLines)
    {
        string title;
        string company;

        if (headerLines.Count >= 2)
        {
            company = headerLines[0];
            title = headerLines[1];
        }
        else if (headerLines.Count == 1)
        {
            var split = TrySplitTitleCompany(headerLines[0]);
            title = split.Title;
            company = split.Company;
        }
        else
        {
            title = string.Empty;
            company = string.Empty;
        }

        return new ExperienceInput
        {
            Title = title.Trim(),
            Company = company.Trim(),
            Description = string.Join("\n", descLines).Trim()
        };
    }

    private static (string Title, string Company) TrySplitTitleCompany(string line)
    {
        var separators = new[] { " · ", " ·", "• ", " at ", " @ ", " na ", " em ", " no ", " nos " };
        foreach (var sep in separators)
        {
            var idx = line.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
            if (idx <= 0)
                continue;

            return (line[..idx].Trim(), line[(idx + sep.Length)..].Trim());
        }

        return (line.Trim(), string.Empty);
    }

    private static List<ExperienceInput> DeduplicateExperiences(List<ExperienceInput> items)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ExperienceInput>();

        foreach (var item in items)
        {
            var key = $"{item.Company}|{item.Title}";
            if (!seen.Add(key))
                continue;

            result.Add(item);
        }

        return result;
    }

    private static bool IsBulletLine(string line) =>
        line.StartsWith('•') || line.StartsWith('·') || line.StartsWith('-') || line.StartsWith('*');

    private static List<ExperienceInput> ParseExperiencesLegacy(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
            return [];

        var blocks = new List<List<string>>();
        List<string> current = [];

        foreach (var line in lines)
        {
            if (DateLineRegex.IsMatch(line) && current.Count > 0)
            {
                current.Add(line);
                blocks.Add(current);
                current = [];
                continue;
            }

            if (current.Count > 0 && current.Any(DateLineRegex.IsMatch) && IsLikelyNewJobHeader(line))
            {
                blocks.Add(current);
                current = [line];
                continue;
            }

            current.Add(line);
        }

        if (current.Count > 0)
            blocks.Add(current);

        return blocks
            .Select(ParseExperienceBlock)
            .Where(e => !string.IsNullOrWhiteSpace(e.Title) || !string.IsNullOrWhiteSpace(e.Company))
            .Take(12)
            .ToList();
    }

    private static bool IsLikelyNewJobHeader(string line) =>
        !DateLineRegex.IsMatch(line) &&
        !LooksLikeLocation(line) &&
        line.Length < 90 &&
        !line.StartsWith('•') &&
        !line.StartsWith('·');

    private static ExperienceInput ParseExperienceBlock(List<string> block)
    {
        var dateIdx = block.FindIndex(DateLineRegex.IsMatch);
        var header = dateIdx > 0 ? block.Take(dateIdx).ToList() : block.Take(Math.Min(3, block.Count)).ToList();
        var tail = dateIdx >= 0 ? block.Skip(dateIdx + 1) : block.Skip(header.Count);
        var description = string.Join("\n", tail.Where(l => !LooksLikeLocation(l) && !DateLineRegex.IsMatch(l)));

        string title;
        string company;

        if (header.Count >= 2)
        {
            // LinkedIn PDF: empresa na 1ª linha, cargo na 2ª
            company = header[0];
            title = header[1];
        }
        else
        {
            title = header.FirstOrDefault() ?? "";
            company = "";
        }

        return new ExperienceInput
        {
            Title = title.Trim(),
            Company = company.Trim(),
            Description = description.Trim()
        };
    }

    /// <summary>Expõe parsing de texto normalizado para testes e diagnóstico.</summary>
    internal static List<ExperienceInput> ParseExperiencesFromText(string rawText) =>
        ParseExperiences(NormalizeLines(rawText));
}
