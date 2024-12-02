using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bibliotech.Models
{
    public class Multa
    {
        public int Id { get; set; }
        public int EmprestimoId { get; set; }
        public decimal Valor { get; set; }
    }
}