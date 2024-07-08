using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Models
{
    internal class Game
    {
        public long Id { get; set; }

        [Column("name")]
        public string? Name { get; set; }
    }
}
