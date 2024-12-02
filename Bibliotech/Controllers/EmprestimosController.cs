using Microsoft.AspNetCore.Mvc;
using Bibliotech.Data;
using Bibliotech.Models;
using Microsoft.EntityFrameworkCore;

namespace Bibliotech.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmprestimosController : ControllerBase
    {
        private readonly BibliotecaContext _context;

        public EmprestimosController(BibliotecaContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Emprestimo>> EmprestarLivro(Emprestimo emprestimo)
        {
            emprestimo.CodigoEmprestimo = Emprestimo.GenerateCodigoEmprestimo(); 
            _context.Emprestimos.Add(emprestimo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmprestimoPorId), new { id = emprestimo.Id }, emprestimo);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Emprestimo>> GetEmprestimoPorId(int id)
        {
            var emprestimo = await _context.Emprestimos.FindAsync(id);

            if (emprestimo == null)
            {
                return NotFound();
            }

            return emprestimo;
        }

 
    }
}