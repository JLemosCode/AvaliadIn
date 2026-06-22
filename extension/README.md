# Extensão AvaliadIN Capture

Permite capturar o perfil LinkedIn **pela URL** usando sua sessão logada no navegador — sem API paga.

## Instalação (Chrome / Edge)

1. Abra `chrome://extensions`
2. Ative **Modo do desenvolvedor**
3. Clique em **Carregar sem compactação**
4. Selecione a pasta `extension/` deste repositório
5. Recarregue o AvaliadIN em http://localhost:3000

## Uso

1. No AvaliadIN, cole a URL: `https://www.linkedin.com/in/seu-perfil`
2. Clique em **Avaliar currículo**
3. A extensão abre o LinkedIn, captura headline, sobre, experiências e skills
4. O relatório RDIS + SSI-JS aparece automaticamente no AvaliadIN

## Requisitos

- Estar **logado** no LinkedIn no mesmo navegador
- API AvaliadIN rodando em http://localhost:5080
- Web AvaliadIN em http://localhost:3000 ou http://localhost:5173

## Avaliação SSI

O score **SSI-JS** segue os [4 pilares do LinkedIn SSI](https://www.linkedin.com/sales/ssi), adaptados para job seekers. Compare com seu score oficial em linkedin.com/sales/ssi após aplicar o plano de ação.
