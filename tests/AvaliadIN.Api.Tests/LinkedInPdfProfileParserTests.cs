using AvaliadIN.Api.LinkedIn;
using Xunit;

namespace AvaliadIN.Api.Tests;

public class LinkedInPdfProfileParserTests
{
    [Fact]
    public void ParseExperiencesFromText_SingleJobPortuguesePdf()
    {
        const string text = """
            Contato
            www.linkedin.com/in/geovanna-sousa-tech-recruiter (LinkedIn)
            Principais competências
            Conhecimentos de CRM
            Geovanna Sousa
            Mentora de Carreiras em Tech | Project Manager
            Campinas e Região
            Experiência
            Times Idiomas Butantã
            Headhunter
            março de 2025 - Present (1 ano 4 meses)
            Campinas, SP
            Formação acadêmica
            """;

        var experiences = LinkedInPdfProfileParser.ParseExperiencesFromText(text);

        Assert.Single(experiences);
        Assert.Equal("Headhunter", experiences[0].Title);
        Assert.Equal("Times Idiomas Butantã", experiences[0].Company);
    }

    [Fact]
    public void ParseExperiencesFromText_MultipleJobsWithAbbreviatedDates()
    {
        const string text = """
            Experiência
            JL LEMOS DIGITAL LTDA
            Consultor Sênior Full Stack
            jan. de 2020 - Present
            • Desenvolvimento full stack em C#, .NET, Angular e SQL Server.
            Ratto Software
            Desenvolvedor Full Stack Sênior
            mar. de 2015 - dez. de 2019
            • Sistemas web em C#, ASP.NET MVC, Angular.
            Formação acadêmica
            """;

        var experiences = LinkedInPdfProfileParser.ParseExperiencesFromText(text);

        Assert.Equal(2, experiences.Count);
        Assert.Equal("Consultor Sênior Full Stack", experiences[0].Title);
        Assert.Equal("JL LEMOS DIGITAL LTDA", experiences[0].Company);
        Assert.Contains("Desenvolvimento full stack", experiences[0].Description);
        Assert.Equal("Desenvolvedor Full Stack Sênior", experiences[1].Title);
        Assert.Equal("Ratto Software", experiences[1].Company);
    }

    [Fact]
    public void ParseExperiencesFromText_GluedPdfPigLayout()
    {
        const string text =
            "ExperiênciaJL LEMOS DIGITAL LTDAConsultor Sênior Full Stackjan. de 2020 - PresentRatto SoftwareDesenvolvedor Full Stack Sêniormar. de 2015 - dez. de 2019Formação acadêmica";

        var experiences = LinkedInPdfProfileParser.ParseExperiencesFromText(text);

        Assert.Equal(2, experiences.Count);
        Assert.Contains(experiences, e => e.Title == "Consultor Sênior Full Stack");
        Assert.Contains(experiences, e => e.Title == "Desenvolvedor Full Stack Sênior");
    }
}
