using System.ComponentModel.DataAnnotations;

namespace Bibliotech.Models
{
    public class Livro
    {
        public int Id { get; set; }

        [Required]
        public string Titulo { get; set; }

        public string ISBN { get; set; }
        public string Autor { get; set; }
        public string Editora { get; set; }
        public string Assunto { get; set; }
        public int Edicao { get; set; }
        public DateTime DataInclusao { get; set; } = DateTime.Now;
        public bool Disponivel { get; set; } = true;
        public ICollection<Reserva> Reservas { get; set; }
    }
}