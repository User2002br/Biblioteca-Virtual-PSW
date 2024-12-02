📚 Biblioteca Virtual - Bibliotech
Bibliotech é um sistema robusto de gerenciamento de biblioteca virtual, desenvolvido com ASP.NET Core e Entity Framework Core. Ele oferece uma experiência completa para gerenciar usuários, livros, reservas, empréstimos e tarefas administrativas.

🚀 Tecnologias Utilizadas
ASP.NET Core: Framework para construção da aplicação web.
Entity Framework Core: Para interações com o banco de dados.
Microsoft SQL Server: Banco de dados principal.
Autenticação e Autorização: Baseada em Cookie Authentication.
Logging: Utilizando Microsoft.Extensions.Logging.
Serialização JSON: Implementada com System.Text.Json.
Injeção de Dependência: Para simplificar o gerenciamento de dependências.

⚙️ Funcionalidades Principais
Gerenciamento de Usuários
Operações CRUD completas.
Sistema de autenticação e autorização.
Controle de acesso baseado em funções:
Admin, Bibliotecário, Usuário Externo, Aluno, Professor.
Gerenciamento de Livros
CRUD para livros.
Marcação automática de disponibilidade:
Livros indisponíveis quando reservados ou emprestados.
Atualização de status ao devolver livros.
Gerenciamento de Empréstimos
Criar, visualizar e gerenciar empréstimos.
Aprovação e rejeição de pedidos.
Geração e codificação de códigos de empréstimo.
Gerenciamento de Reservas
Sistema integrado de reservas.
Detalhamento completo das reservas.
Tarefas Administrativas
Monitorar e gerenciar empréstimos e reservas.
Bloqueio/desbloqueio de usuários com base no histórico de uso e multas.

🗂️ Estrutura do Projeto
Controllers
Manipulam as requisições e respostas HTTP.

UsuariosController: Gerenciamento de usuários.
LivrosController: Operações relacionadas a livros.
EmprestimosController: Gestão de empréstimos.
ReservasController: Controle de reservas.
PedidosController: Aprovações de pedidos de empréstimo.
HomeController: Sessões e páginas gerais da aplicação.
Models
Definem as estruturas de dados:

Usuario: Estrutura para usuários.
Livro: Estrutura para livros.
Emprestimo: Estrutura para empréstimos.
Reserva: Estrutura para reservas.
Pedido: Estrutura para pedidos de empréstimo.
Data
Configurações do banco de dados:

BibliotecaContext: Contexto do Entity Framework Core.
Views
Visualizações Razor para renderizar páginas dinâmicas em HTML.

🛠️ Configuração e Execução
Pré-requisitos:

.NET 8.0 SDK instalado.
Banco de dados configurado no Microsoft SQL Server.
Configuração:

Atualize a string de conexão do banco de dados em appsettings.json.

Execute no terminal:

dotnet restore

dotnet ef database update

dotnet run

Como acessar o projeto:
Abra o navegador e acesse o localhost fornecido no terminal de sua máquina.
