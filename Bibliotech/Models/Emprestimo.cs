using Microsoft.Identity.Client;
using System;

namespace Bibliotech.Models
{
    public class Emprestimo
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public Usuario usuario { get; set; }
        public int LivroId { get; set; }
        public Livro Livro { get; set; }
        public DateTime DataEmprestimo { get; set; } = DateTime.Now;
        public DateTime? DataDevolucao { get; set; }
        public int MultaPaga { get; set; } = 10;
        public DateTime? MultaDataValidade { get; set; } = DateTime.Now;
        public string CodigoEmprestimo { get; set; } = GenerateCodigoEmprestimo();

        public static string GenerateCodigoEmprestimo() 
        {
            var codigo = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(); 
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(codigo)); 
        }
    }
}