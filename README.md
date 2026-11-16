# A Place Among Lords

**A Place Among Lords** é um RPG narrativo medieval de baixa fantasia, desenvolvido em Unity e C# como protótipo de Trabalho de Conclusão de Curso em Ciência da Computação.

O jogador assume o papel de um jovem nobre da família Mondna, que herda as terras do pai durante uma rebelião contra o rei. Em cinco rodadas, suas decisões moldam os recursos do domínio, as relações com personagens e facções e a preparação para a batalha. O protótipo termina antes do início do combate.

## O protótipo

- Interface 2D de uma sala principal com personagens e objetos interativos.
- Diálogos com respostas que podem alterar relacionamentos.
- Distribuição inicial de pontos em Diplomacy, Warfare, Management e Combat.
- Recursos do domínio: Influence, Treasury, Supplies, Reputation e Troops.
- Cinco eventos narrativos, com escolhas condicionais, efeitos persistentes e consequências.
- Salvamento e carregamento da partida.
- Interface do jogo em inglês.

A apresentação visual atual é uma interface funcional de protótipo; arte e polimento visual podem ser desenvolvidos em etapas futuras.

## Jogar no Windows

1. Abra a página [Releases](https://github.com/DominicLaercio/A-Place-Among-Lords/releases) e baixe o arquivo ZIP da versão para Windows.
2. Extraia o ZIP para uma pasta.
3. Abra a pasta extraída e execute `A Place Among Lords.exe`.
4. Mantenha o executável junto dos arquivos e pastas que vieram no ZIP.

Não é necessário instalar a Unity para jogar a build. O jogo começa no menu principal. Escolha **Start New Game**, distribua os oito pontos de atributos e avance manualmente pelas rodadas. Clique nos personagens ou objetos da sala para consultar informações e conversar; as opções de diálogo e de evento aparecem na interface. Use **Game Menu** para salvar, carregar, retomar, voltar ao menu principal ou sair. Ao concluir a quinta rodada, o jogo mostra o encerramento do protótipo e permite retornar ao menu ou sair.

## Abrir o projeto no Unity

- Versão do Editor: **Unity 6000.2.8f1**.
- Plataforma da build de demonstração: **Windows x86-64**.
- Abra a pasta deste repositório pelo Unity Hub e carregue `Assets/Scenes/Game.unity`.

### Organização principal

- `Assets/Scripts/Data`: modelos de atributos, recursos, relacionamentos, eventos e diálogos.
- `Assets/Scripts/Core`: estado da partida, regras, validação de catálogo, salvamento e controlador do fluxo.
- `Assets/Resources/Data/Events`: eventos e escolhas configurados como ScriptableObjects.
- `Assets/Resources/Data/Conversations`: diálogos e respostas configurados como ScriptableObjects.
- `Assets/Tests/EditMode`: testes EditMode dos sistemas centrais.
- `Documentation`: documentação de arquitetura e escopo do protótipo.

Eventos e diálogos são dados editáveis em ScriptableObjects; o código C# avalia condições, aplica efeitos e mantém o estado persistente da partida.

## Estado da build

A build para Windows é distribuída separadamente do código-fonte, na seção Releases. O repositório contém o projeto Unity; a pasta local `Build` é gerada pelo Editor e não faz parte do controle de versão.
