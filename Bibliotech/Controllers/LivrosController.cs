using Microsoft.AspNetCore.Mvc;
using Bibliotech.Data;
using Bibliotech.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bibliotech.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LivrosController : ControllerBase
    {
        private readonly BibliotecaContext _context;
        private readonly ILogger<LivrosController> _logger;

        public LivrosController(BibliotecaContext context, ILogger<LivrosController> logger) // Update constructor
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<Livro>> CadastrarLivro(Livro livro)
        {
            _context.Livros.Add(livro);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLivroPorId), new { id = livro.Id }, livro);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Livro>> GetLivroPorId(int id)
        {
            var livro = await _context.Livros.FindAsync(id);

            if (livro == null)
            {
                return NotFound();
            }

            return livro;
        }

        [HttpPost("inutilizar/{id}")]
        public async Task<IActionResult> InutilizarLivro(int id)
        {
            var livro = await _context.Livros.FindAsync(id);

            if (livro == null)
            {
                _logger.LogWarning("Livro with ID {Id} not found.", id);
                return NotFound();
            }

            if (!livro.Disponivel)
            {
                _logger.LogInformation("Livro with ID {Id} is already indisponível.", id);
                return BadRequest("Livro já está indisponível.");
            }

            livro.Disponivel = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Livro with ID {Id} has been reservado e marcado como indisponível.", id);

            return Ok(new { success = true });
        }

        [HttpPost("DevolverLivro")]
        public async Task<IActionResult> DevolverLivro([FromBody] int livroId)
        {
            _logger.LogInformation("DevolverLivro called with livroId: {livroId}", livroId);

            var livro = await _context.Livros.FindAsync(livroId);
            if (livro == null)
            {
                _logger.LogWarning("Livro with ID {livroId} not found.", livroId);
                return NotFound();
            }

            var emprestimo = await _context.Emprestimos.FirstOrDefaultAsync(e => e.LivroId == livroId);
            if (emprestimo == null)
            {
                _logger.LogWarning("Emprestimo for Livro with ID {livroId} not found. Ignoring.", livroId);
            }
            else
            {
                _context.Emprestimos.Remove(emprestimo);
            }

            livro.Disponivel = true;

            _context.Livros.Update(livro);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Livro with ID {livroId} successfully returned.", livroId);
            return Ok();
        }
    }
}