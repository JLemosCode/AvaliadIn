interface LoadingPanelProps {
  phase?: 'evaluate' | 'ai'
}

export function LoadingPanel({ phase = 'evaluate' }: LoadingPanelProps) {
  const isAi = phase === 'ai'

  return (
    <div className="loading-panel">
      <div className="loading-panel__spinner" aria-hidden="true" />
      <h2>{isAi ? 'Validando currículo com IA...' : 'Calculando RDIS e SSI-JS...'}</h2>
      <p>
        {isAi
          ? 'Simulando busca de recrutadores e gerando sugestões de mercado para o seu perfil.'
          : 'Analisando headline, experiências, skills e os 4 pilares do LinkedIn SSI.'}
      </p>
      {isAi && (
        <p className="loading-panel__sub">Isso pode levar alguns segundos.</p>
      )}
    </div>
  )
}
