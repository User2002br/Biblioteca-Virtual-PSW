using System;
using System.ComponentModel.DataAnnotations;

namespace Bibliotech.Models
{
    public class Reserva
    {
        public int Id { get; set; }

        [Required]
        public int LivroId { get; set; }

        public Livro Livro { get; set; }

        public DateTime DataReserva { get; set; } = DateTime.Now;

    }
}