using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace Bibliotech.Models
{
    public class Usuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 
        public string Nome { get; set; }
        public string Login { get; set; }
        public string Senha { get; set; }
        public string Perfil { get; set; }
        public bool Bloqueado { get; set; }
        public string MotivoBloqueio { get; set; }
        public int QuantasMultas { get; set; }
        public int QuantasVezesBloqueado { get; set; }
        public DateTime DataDesbloqueio { get; set; }
        public DateTime DataQuandoBloqueado { get; set; }

        public static int GenerateRandomId()
        {
            var randomBytes = new byte[4];
            RandomNumberGenerator.Fill(randomBytes);
            return BitConverter.ToInt32(randomBytes, 0);
        }
    }
}