using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Models
{
    [Table("devices")]
    internal class Device
    {
        [Column("id")]
        public string? Id { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("last_online")]
        public DateTime LastOnline { get; set; }

    }
}
