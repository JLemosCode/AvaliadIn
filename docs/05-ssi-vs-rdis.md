# SSI vs RDIS — visão do AvaliadIN

## Objetivo do projeto

Ajudar o usuário a **melhorar o currículo LinkedIn** de acordo com o mercado:

1. **RDIS** — validação para buscas de recrutadores, ATS e assistentes de contratação (IA de hiring)
2. **SSI-JS** — mesmos [4 pilares do LinkedIn SSI](https://www.linkedin.com/sales/ssi), adaptados para quem busca emprego
3. **IA (opcional)** — coaching e simulação de como o perfil aparece em buscas

## Comparativo com o SSI oficial

| | [LinkedIn SSI](https://www.linkedin.com/sales/ssi) | AvaliadIN SSI-JS |
|---|---------------------------------------------------|------------------|
| Mede | Atividade social real (90 dias) | Estimativa a partir do perfil + dados informados |
| Pilares | 4 × 25 pts | 4 × 25 pts (mesmos nomes) |
| Público | Sales Navigator | Job seekers |
| Currículo | Indireto | **RDIS** direto (headline, about, skills…) |

O AvaliadIN **não substitui** o SSI oficial — use os dois:

- **Aqui** → planejar melhorias no texto e na rotina
- **[linkedin.com/sales/ssi](https://www.linkedin.com/sales/ssi)** → score real após 7+ dias de atividade

## Regra de ouro

| Métrica | O que melhorar |
|---------|----------------|
| **RDIS baixo** | Headline, Sobre, experiências, skills, Open to Work |
| **SSI-JS baixo** | Posts, comentários, buscas, convites, foto, recomendações |
| **SSI oficial baixo** | Rotina social consistente (não só reescrever o perfil) |

Perfil otimizado = RDIS alto. Rotina diária = SSI alto.

## Fluxo recomendado

1. Importar perfil (PDF ou formulário)
2. Avaliar → painel **SSI-JS** (4 pilares) + **RDIS** (mercado)
3. Aplicar plano de ação da semana
4. **Validar com IA** (keywords + busca simulada de recrutador)
5. Publicar mudanças no LinkedIn
6. Comparar SSI oficial após 7 dias

## Validação manual

1. LinkedIn → Visualizar como membro público
2. Copiar texto das seções
3. `POST /api/v1/evaluate`
4. Checar [SSI oficial](https://www.linkedin.com/sales/ssi) após rotina
