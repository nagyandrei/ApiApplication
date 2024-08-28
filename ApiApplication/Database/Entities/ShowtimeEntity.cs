using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ApiApplication.Database.Entities
{
    public class ShowtimeEntity
    {
        public int Id { get; set; }
        [JsonIgnore]

        public MovieEntity Movie { get; set; }
        public DateTime SessionDate { get; set; }
        public int AuditoriumId { get; set; }
        [JsonIgnore]

        public ICollection<TicketEntity> Tickets { get; set; }
    }
}
