using System;
using System.Text.Json.Serialization;

namespace ApiApplication.Database.Entities
{
    public class ReservationEntity
    {
        public Guid Id { get; set; }
        public int NoOfSeats { get; set; }
        public int AuditoriumId { get; set; }
        [JsonIgnore]

        public AuditoriumEntity Auditorium { get; set; } 
        public int MovieId { get; set; }
        [JsonIgnore]

        public MovieEntity Movie { get; set; } 
        public DateTime CreatedTime { get; set; }
        public Guid TicketId { get; set; }
    }

}
