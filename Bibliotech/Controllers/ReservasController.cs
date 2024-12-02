using Microsoft.AspNetCore.Mvc;
using Bibliotech.Data;
using Bibliotech.Models;
using Microsoft.EntityFrameworkCore;

namespace Bibliotech.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservasController : ControllerBase
    {
        private readonly BibliotecaContext _context;

        public ReservasController(BibliotecaContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult ReservarLivro(int LivroId)
        {
            var livro = _context.Livros.Find(LivroId);
            if (livro == null || !livro.Disponivel)
            {
                return RedirectToAction("MenuBibliotecario");
            }

            var reserva = new Reserva
            {
                LivroId = LivroId,
                DataReserva = DateTime.Now,
            };

            _context.Reservas.Add(reserva);
            livro.Disponivel = false;
            _context.SaveChanges();

            return RedirectToAction("MenuBibliotecario");
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Reserva>> GetReservaPorId(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);

            if (reserva == null)
            {
                return NotFound();
            }

            return reserva;
        }
    }
}