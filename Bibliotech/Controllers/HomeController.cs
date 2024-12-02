using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Bibliotech.Models;
using Bibliotech.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Security.Claims; 
using Microsoft.AspNetCore.Authentication; 
using Microsoft.AspNetCore.Authentication.Cookies; 
namespace Bibliotech.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly BibliotecaContext _context;

    public HomeController(ILogger<HomeController> logger, BibliotecaContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost]
    [HttpGet]
    public IActionResult MainMenu()
    {
        return RedirectToAction("Menu");
    }

    public IActionResult Menu()
    {
        return View("Menu");
    }

    public IActionResult Logout()
    {
        return RedirectToAction("Index");
    }

    public IActionResult Admin()
    {
        return View();
    }

    public IActionResult Bibliotecario()
    {
        var livros = _context.Livros.ToList();
        return View(livros);
    }

    public async Task<IActionResult> MenuUsuario()
    {
        var userId = User.FindFirst("Id")?.Value;
        if (userId == null)
        {
            return RedirectToAction("Index");
        }

        var emprestimos = await _context.Emprestimos
            .Include(e => e.Livro)
            .Where(e => e.UsuarioId == int.Parse(userId))
            .ToListAsync();
        
        int totalMultas = emprestimos.Count(e => e.DataDevolucao < DateTime.Now);

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
        if (usuario != null)
        {
            usuario.QuantasMultas = totalMultas;

            if (totalMultas >= 2 && !usuario.Bloqueado)
            {
                usuario.Bloqueado = true;
                usuario.QuantasVezesBloqueado += 1;
                usuario.MotivoBloqueio = "Multas Excessivas.";
                usuario.DataQuandoBloqueado = DateTime.Now;

                if (usuario.QuantasVezesBloqueado >= 5)
                {
                    usuario.DataDesbloqueio = DateTime.MaxValue; 
                }
                else
                {
                    usuario.DataDesbloqueio = DateTime.Now.AddDays(14 * usuario.QuantasVezesBloqueado); // Bloqueado por 2 semanas
                }
            }

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            DateTime multaDataValidade = usuario.DataDesbloqueio;
            ViewBag.MultaDataValidade = multaDataValidade;

            var identity = (ClaimsIdentity)User.Identity;
            var quantasMultasClaim = identity.FindFirst("QuantasMultas");
            if (quantasMultasClaim != null)
            {
                identity.RemoveClaim(quantasMultasClaim);
            }
            identity.AddClaim(new Claim("QuantasMultas", totalMultas.ToString()));
            identity.AddClaim(new Claim("Bloqueado", usuario.Bloqueado.ToString()));
            identity.AddClaim(new Claim("MotivoBloqueio", usuario.MotivoBloqueio ?? ""));
            identity.AddClaim(new Claim("DataDesbloqueio", usuario.DataDesbloqueio.ToString("o")));
            identity.AddClaim(new Claim("DataQuandoBloqueado", usuario.DataQuandoBloqueado.ToString("o")));
            
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        return View(emprestimos);
    }

    public IActionResult Discover()
    {
        var livros = _context.Livros.ToList();
        var userLogin = User.Identity.IsAuthenticated ? User.FindFirst(ClaimTypes.Email)?.Value : null;

        List<int> borrowedBookIds = new List<int>();
        if (!string.IsNullOrEmpty(userLogin))
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Login == userLogin);
            if (usuario != null)
            {
                int usuarioId = usuario.Id;

                var emprestimos = _context.Emprestimos
                    .Where(e => e.UsuarioId == usuarioId && e.DataDevolucao == null)
                    .Select(e => e.LivroId)
                    .ToList();

                var pedidosDoUsuario = _context.Pedidos
                    .Where(p => p.UsuarioId == usuarioId)
                    .Select(p => p.LivroId)
                    .ToList();

                borrowedBookIds.AddRange(emprestimos);
                borrowedBookIds.AddRange(pedidosDoUsuario);
                borrowedBookIds = borrowedBookIds.Distinct().ToList();

                _logger.LogInformation("Usuário: {UsuarioId} possui os seguintes LivroIds emprestados ou pedidos: {LivroIds}", 
                    usuarioId, string.Join(", ", borrowedBookIds));
            }
            else
            {
                _logger.LogWarning("Usuário com login {UserLogin} não encontrado.", userLogin);
            }
        }
        else
        {
            _logger.LogInformation("Usuário não autenticado acessou Discover.");
        }

        ViewBag.BorrowedBooks = borrowedBookIds;
        return View(livros);
    }

    public IActionResult UsersPage()
    {
        var usuarios = _context.Usuarios.ToList();
        
        foreach(var usuario in usuarios)
        {
            _logger.LogInformation($"User ID: {usuario.Id}, Nome: {usuario.Nome}");
        }
        
        return View("UsersPage", usuarios);
    }

    public IActionResult MenuBibliotecario()
    {
        var livros = _context.Livros.ToList();
        var emprestimos = _context.Emprestimos.Include(e => e.Livro).ToList(); 
        var pedidos = _context.Pedidos
            .Include(p => p.Usuario)
            .Include(p => p.Livro)
            .ToList();
        ViewBag.Pedidos = pedidos;
        ViewBag.Emprestimos = emprestimos; 
        return View("MenuBibliotecario", livros);
    }

    [HttpPost]
    public IActionResult EmprestarLivro(string UserLogin, int LivroId)
    {
        var usuario = _context.Usuarios.FirstOrDefault(u => u.Login == UserLogin);
        if (usuario == null)
        {
            TempData["ErrorMessage"] = "Login do usuário inválido.";
            return RedirectToAction("MenuBibliotecario");
        }

        var emprestimosAtuais = _context.Emprestimos.Count(e => e.UsuarioId == usuario.Id);
        int limiteEmprestimos = usuario.Perfil switch
        {
            "Bibliotecario" => 0,
            "Usuario Externo" => 1,
            "Aluno" => 2,
            "Professor" => 5,
            "Admin" => int.MaxValue,
            _ => 0
        };

        if (emprestimosAtuais >= limiteEmprestimos)
        {
            TempData["ErrorMessage"] = "Usuário atingiu o limite de livros emprestados.";
            return RedirectToAction("MenuBibliotecario");
        }

        var livro = _context.Livros.FirstOrDefault(l => l.Id == LivroId && l.Disponivel);
        if (livro == null)
        {
            TempData["ErrorMessage"] = "ID do livro inválido ou livro indisponível.";
            return RedirectToAction("MenuBibliotecario");
        }

        livro.Disponivel = false;

        var emprestimo = new Emprestimo
        {
            UsuarioId = usuario.Id,
            LivroId = livro.Id,
            DataEmprestimo = DateTime.Now,
            DataDevolucao = DateTime.Now.AddDays(14),
            MultaPaga = 10
        };

        _context.Emprestimos.Add(emprestimo);
        _context.SaveChanges();

        return RedirectToAction("MenuBibliotecario");
    }

    [HttpPost]
    public async Task<IActionResult> DevolverLivro(int LivroId)
    {
        var livro = await _context.Livros.FirstOrDefaultAsync(l => l.Id == LivroId && !l.Disponivel);
        if (livro == null)
        {
            return Json(new { success = false, message = "ID do livro inválido ou livro já está disponível." });
        }

        var emprestimo = await _context.Emprestimos.FirstOrDefaultAsync(e => e.LivroId == LivroId);
        if (emprestimo != null)
        {
            _context.Emprestimos.Remove(emprestimo); 
        }

        livro.Disponivel = true;
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    public IActionResult Bloqueado()
    {
        var userId = User.FindFirst("Id")?.Value;
        if (userId == null)
        {
            return RedirectToAction("Index");
        }

        var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == int.Parse(userId));
        if (usuario == null)
        {
            return RedirectToAction("Index");
        }

        ViewBag.IsPermanentlyBlocked = usuario.DataDesbloqueio == DateTime.MaxValue;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}