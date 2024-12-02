using Microsoft.AspNetCore.Mvc;
using Bibliotech.Data;
using Bibliotech.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace Bibliotech.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class PedidosController : ControllerBase
    {
        private readonly BibliotecaContext _context;
        private readonly ILogger<PedidosController> _logger;

        public PedidosController(BibliotecaContext context, ILogger<PedidosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("CadastrarPedido")]
        public async Task<ActionResult<Pedido>> CadastrarPedido([FromBody] Pedido pedido)
        {
            _logger.LogInformation("Iniciando CadastrarPedido com PedidoId: {PedidoId}", pedido?.Id);

            if (pedido == null)
            {
                _logger.LogError("Pedido é nulo.");
                return BadRequest("Dados do pedido não fornecidos.");
            }

            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogError("Usuário não autenticado.");
                return Unauthorized("Usuário não autenticado.");
            }

            var userLogin = User.FindFirst(ClaimTypes.Email)?.Value;
            _logger.LogInformation("Usuário autenticado: {UserLogin}", userLogin);

            if (string.IsNullOrEmpty(userLogin))
            {
                _logger.LogError("Usuário não autenticado.");
                return Unauthorized("Usuário não autenticado.");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Login == userLogin);
            if (usuario == null)
            {
                _logger.LogError("Usuário não encontrado. Login: {Login}", userLogin);
                _logger.LogError("Verifique se o login está correto e se o usuário existe no banco de dados.");
                return BadRequest("Usuário não encontrado.");
            }

            pedido.UsuarioId = usuario.Id;
            _logger.LogInformation("Definido UsuarioId: {UsuarioId} para o Pedido", usuario.Id);

            if (!ModelState.IsValid)
            {
                _logger.LogError("Modelo inválido: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Pedido recebido: LivroId = {LivroId}, UsuarioId = {UsuarioId}", pedido.LivroId, pedido.UsuarioId);

            if (pedido.LivroId <= 0 || pedido.UsuarioId <= 0)
            {
                _logger.LogError("LivroId ou UsuarioId inválido. LivroId: {LivroId}, UsuarioId: {UsuarioId}", pedido.LivroId, pedido.UsuarioId);
                return BadRequest("LivroId ou UsuarioId inválido.");
            }

            var livro = await _context.Livros.FindAsync(pedido.LivroId);
            if (livro == null)
            {
                _logger.LogError("Livro não encontrado. LivroId: {LivroId}", pedido.LivroId);
                return BadRequest("Livro não encontrado.");
            }

            var pedidosDoUsuario = await _context.Pedidos.CountAsync(p => p.UsuarioId == pedido.UsuarioId);
            _logger.LogInformation("Usuário já possui {Count} pedidos.", pedidosDoUsuario);

            if (pedidosDoUsuario >= 2)
            {
                _logger.LogWarning("Usuário {UsuarioId} atingiu o limite de pedidos.", pedido.UsuarioId);
                return BadRequest("O usuário já possui 2 pedidos.");
            }

            if (!livro.Disponivel)
            {
                _logger.LogWarning("Livro {LivroId} não está disponível.", pedido.LivroId);
                return BadRequest("O livro não está disponível.");
            }

            _context.Pedidos.Add(pedido);
            _logger.LogInformation("Pedido adicionado ao contexto.");

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Alterações salvas no banco de dados.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar alterações no banco de dados.");
                return StatusCode(500, "Erro interno do servidor.");
            }

            _logger.LogInformation("Pedido criado com sucesso. PedidoId: {PedidoId}", pedido.Id);
            return CreatedAtAction(nameof(GetPedidoPorId), new { id = pedido.Id }, pedido);
        }

        [HttpPost("AprovarPedido")]
        public async Task<IActionResult> AprovarPedido([FromBody] AprovarPedidoRequest request)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Livro)
                .FirstOrDefaultAsync(p => p.Id == request.PedidoId);

            if (pedido == null)
            {
                return NotFound("Pedido não encontrado.");
            }

            if (!pedido.Livro.Disponivel)
            {
                return BadRequest("O livro solicitado já está indisponível.");
            }

            var usuario = await _context.Usuarios.FindAsync(pedido.UsuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado.");
            }

            int limiteLivros;
            switch (usuario.Perfil)
            {
                case "Bibliotecário":
                    limiteLivros = 0;
                    break;
                case "Usuario Externo":
                    limiteLivros = 1;
                    break;
                case "Aluno":
                    limiteLivros = 2;
                    break;
                case "Professor":
                    limiteLivros = 5;
                    break;
                case "Admin":
                    limiteLivros = int.MaxValue;
                    break;
                default:
                    return BadRequest("Perfil de usuário desconhecido.");
            }

            var emprestimosDoUsuario = await _context.Emprestimos.CountAsync(e => e.UsuarioId == pedido.UsuarioId);
            if (emprestimosDoUsuario >= limiteLivros)
            {
                return BadRequest($"O usuário já possui {limiteLivros} livros emprestados.");
            }

            string codigoEmprestimo = GenerateCodigoEmprestimo();

            string codigoEmprestimoCodificado = EncodeCodigo(codigoEmprestimo);

            var emprestimo = new Emprestimo
            {
                UsuarioId = pedido.UsuarioId,
                LivroId = pedido.LivroId,
                DataEmprestimo = DateTime.Now,
                DataDevolucao = DateTime.Now.AddDays(14),
                MultaPaga = 10,
                CodigoEmprestimo = codigoEmprestimoCodificado
            };

            _context.Emprestimos.Add(emprestimo);

            pedido.Livro.Disponivel = false;

            _context.Pedidos.Remove(pedido);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, emprestimoId = emprestimo.Id, codigoEmprestimo = codigoEmprestimo });
        }

        [HttpPost("RecusarPedido")]
        public async Task<IActionResult> RecusarPedido([FromBody] AprovarPedidoRequest request)
        {
            var pedido = await _context.Pedidos.FindAsync(request.PedidoId);

            if (pedido == null)
            {
                return NotFound("Pedido não encontrado.");
            }

            _context.Pedidos.Remove(pedido);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("AutenticarBibliotecario")]
        public async Task<IActionResult> AutenticarBibliotecario([FromBody] AutenticacaoRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Login) || string.IsNullOrEmpty(request.Senha))
            {
                return BadRequest(new { autenticado = false });
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Login == request.Login && u.Perfil == "Bibliotecario");

            if (usuario == null)
            {
                _logger.LogWarning("Tentativa de autenticação falhou para login: {Login}", request.Login);
                return Ok(new { autenticado = false });
            }

            bool senhaValida = VerificarSenha(request.Senha, usuario.Senha); 

            if (senhaValida)
            {
                _logger.LogInformation("Bibliotecário autenticado: {Login}", request.Login);
                return Ok(new { autenticado = true });
            }
            else
            {
                _logger.LogWarning("Senha inválida para login: {Login}", request.Login);
                return Ok(new { autenticado = false });
            }
        }

        private bool VerificarSenha(string senha, string senhaArmazenada)
        {
            return senha == senhaArmazenada;
        }

        public class AutenticacaoRequest
        {
            public string Login { get; set; }
            public string Senha { get; set; }
        }

        private string GenerateCodigoEmprestimo()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            using (var rng = RandomNumberGenerator.Create()) 
            {
                var data = new byte[8];
                rng.GetBytes(data);
                var result = new char[8];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = chars[data[i] % chars.Length];
                }
                return new string(result);
            }
        }

        private string EncodeCodigo(string codigo)
        {
            var bytes = Encoding.UTF8.GetBytes(codigo);
            return Convert.ToBase64String(bytes);
        }

        public class AprovarPedidoRequest
        {
            public int PedidoId { get; set; }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Pedido>> GetPedidoPorId(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Livro)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            var userLogin = User.FindFirst(ClaimTypes.Email)?.Value;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Login == userLogin);

            if (usuario == null || (pedido.UsuarioId != usuario.Id && usuario.Perfil != "Admin" && usuario.Perfil != "Bibliotecario"))
            {
                return Forbid();
            }

            return pedido;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarPedido(int id, Pedido pedido)
        {
            if (id != pedido.Id)
            {
                return BadRequest();
            }

            _context.Entry(pedido).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PedidoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.Id == id);
        }
    }
}