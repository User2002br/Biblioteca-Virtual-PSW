using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bibliotech.Data;
using Bibliotech.Models; 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace Bibliotech.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : Controller
    {
        private readonly BibliotecaContext _context;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(BibliotecaContext context, ILogger<UsuariosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuarioPorId(int id)
        {
            if (_context.Usuarios == null)
            {
                return Problem("Conjunto de dados 'Usuarios' está nulo.");
            }

            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            return usuario;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            if (_context.Usuarios == null)
            {
                return Problem("Conjunto de dados 'Usuarios' está nulo.");
            }

            return await _context.Usuarios.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Usuario>> CadastrarUsuario(Usuario usuario)
        {
            if (_context.Usuarios == null)
            {
                return Problem("Conjunto de dados 'Usuarios' está nulo.");
            }

            usuario.Id = 0;
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUsuarioPorId), new { id = usuario.Id }, usuario);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarUsuario(int id, Usuario usuario)
        {
            if (id != usuario.Id)
            {
                return BadRequest("O ID fornecido não corresponde ao ID do usuário.");
            }

            if (_context.Usuarios == null)
            {
                return Problem("Conjunto de dados 'Usuarios' está nulo.");
            }

            _context.Entry(usuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Usuarios.Any(e => e.Id == id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletarUsuario(int id)
        {
            if (_context.Usuarios == null)
            {
                return Problem("Conjunto de dados 'Usuarios' está nulo.");
            }

            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("VerificarLogin")]
        public async Task<IActionResult> VerificarLogin(Usuario usuario)
        {
            if (usuario == null || string.IsNullOrEmpty(usuario.Login) || string.IsNullOrEmpty(usuario.Senha))
            {
                return BadRequest("Usuário ou senha não podem ser nulos.");
            }

            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Login == usuario.Login && u.Senha == usuario.Senha);

            if (user == null)
            {
                return Unauthorized("Login ou senha inválidos.");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Nome),
                new Claim(ClaimTypes.Email, user.Login),
                new Claim("Id", user.Id.ToString()),
                new Claim("Nome", user.Nome),
                new Claim("Bloqueado", user.Bloqueado.ToString()),
                new Claim("DataDesbloqueio", user.DataDesbloqueio.ToString("o")),
                new Claim("DataQuandoBloqueado", user.DataQuandoBloqueado.ToString("o")),
                new Claim("MotivoBloqueio", user.MotivoBloqueio ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return Ok(new { success = true });
        }

        [HttpPost("VerificarLoginAdmin")]
        public async Task<IActionResult> VerificarLoginAdmin([FromBody] Usuario usuario)
        {
            if (_context.Usuarios == null)
            {
                return Problem("Conjunto de dados 'Usuarios' está nulo.");
            }

            _logger.LogInformation("Verificando login para o usuário: {Login}", usuario.Login);

            var usuarioEncontrado = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Login == usuario.Login && u.Senha == usuario.Senha && u.Perfil == "Admin");

            if (usuarioEncontrado == null)
            {
                _logger.LogWarning("Login inválido para o usuário: {Login}", usuario.Login);
                return Unauthorized("Login inválido.");
            }

            _logger.LogInformation("Login bem-sucedido para o usuário: {Login}", usuario.Login);
            return Ok("Login bem-sucedido.");
        }

        [HttpPost("VerificarLoginBibliotecario")]
        public async Task<IActionResult> VerificarLoginBibliotecario([FromBody] Usuario usuario)
        {
            if (_context.Usuarios == null)
            {
                return Problem("Conjunto de dados 'Usuarios' está nulo.");
            }

            _logger.LogInformation("Verificando login para o usuário: {Login}", usuario.Login);

            var usuarioEncontrado = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Login == usuario.Login && u.Senha == usuario.Senha);

            if (usuarioEncontrado == null || usuarioEncontrado.Perfil != "Bibliotecário")
            {
                _logger.LogWarning("Login inválido ou sem permissão de bibliotecário para o usuário: {Login}", usuario.Login);
                return Json(new { success = false, message = "Login inválido ou sem permissão de bibliotecário." });
            }

            _logger.LogInformation("Login bem-sucedido para o usuário: {Login}", usuario.Login);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuarioEncontrado.Login),
                new Claim(ClaimTypes.Role, usuarioEncontrado.Perfil),
                new Claim("Id", usuarioEncontrado.Id.ToString()), 
                new Claim("Nome", usuarioEncontrado.Nome),
                new Claim("Bloqueado", usuarioEncontrado.Bloqueado.ToString()),
                new Claim("DataDesbloqueio", usuarioEncontrado.DataDesbloqueio.ToString("o")), 
                new Claim("DataQuandoBloqueado", usuarioEncontrado.DataQuandoBloqueado.ToString("o")), 
                new Claim("MotivoBloqueio", usuarioEncontrado.MotivoBloqueio ?? "") 
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return Json(new { success = true });
        }

        [HttpPost("VerificarLoginExistente")]
        public async Task<IActionResult> VerificarLoginExistente([FromBody] Usuario usuario)
        {
            if (_context.Usuarios == null)
            {
                return Problem("Conjunto de dados 'Usuarios' está nulo.");
            }

            var loginExistente = await _context.Usuarios
                .AnyAsync(u => u.Login == usuario.Login);

            return Ok(loginExistente);
        }

        [HttpPost("CriarUsuarioAleatorio")]
        public async Task<IActionResult> CriarUsuarioAleatorio()
        {
            var randomId = Usuario.GenerateRandomId();
            var usuario = new Usuario
            {
                Nome = $"Usuário {randomId}",
                Login = $"user{randomId}@gmail.com",
                Senha = $"user{randomId}convidado",
                Perfil = "Usuario Externo",
                Bloqueado = false,
                DataDesbloqueio = DateTime.Now,
                DataQuandoBloqueado = DateTime.Now,
                QuantasMultas = 0,
                MotivoBloqueio = null
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Login),
                new Claim(ClaimTypes.Role, usuario.Perfil),
                new Claim("Id", usuario.Id.ToString()), 
                new Claim("Nome", usuario.Nome),
                new Claim("Bloqueado", usuario.Bloqueado.ToString()),
                new Claim("DataDesbloqueio", usuario.DataDesbloqueio.ToString("o")), 
                new Claim("DataQuandoBloqueado", usuario.DataQuandoBloqueado.ToString("o")),
                new Claim("MotivoBloqueio", usuario.MotivoBloqueio ?? "") 
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return Json(new { success = true, usuario });
        }

        [HttpPost("DesbloquearUsuarioById")]
        public async Task<IActionResult> DesbloquearUsuario([FromBody] DesbloquearUsuarioRequest request)
        {
            if (request.UserId <= 0)
            {
                return BadRequest("ID do usuário inválido.");
            }

            var usuario = await _context.Usuarios.FindAsync(request.UserId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado.");
            }

            usuario.Bloqueado = false;
            usuario.DataDesbloqueio = DateTime.Now;
            usuario.DataQuandoBloqueado = DateTime.Now;
            usuario.MotivoBloqueio = null;

            var emprestimos = await _context.Emprestimos.Where(e => e.UsuarioId == request.UserId).ToListAsync();
            _context.Emprestimos.RemoveRange(emprestimos);

            _context.Entry(usuario).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok("Usuário desbloqueado com sucesso.");
        }

        public class DesbloquearUsuarioRequest
        {
            public int UserId { get; set; }
        }

        [HttpGet("ObterCodigoEmprestimo/{livroId}")]
        [Authorize(Roles = "Bibliotecário")] 
        public async Task<IActionResult> ObterCodigoEmprestimo(int livroId)
        {
            if (_context.Emprestimos == null)
            {
                return Problem("Conjunto de dados 'Emprestimos' está nulo.");
            }

            var emprestimo = await _context.Emprestimos.FirstOrDefaultAsync(e => e.LivroId == livroId);
            if (emprestimo == null || string.IsNullOrEmpty(emprestimo.CodigoEmprestimo))
            {
                return NotFound();
            }

            var user = HttpContext.User;
            if (user == null || !user.IsInRole("Bibliotecário"))
            {
                return Forbid();
            }

            var codigoDecodificado = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emprestimo.CodigoEmprestimo));
            return Json(new { codigoEmprestimo = codigoDecodificado });
        }
    }
}