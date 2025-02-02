﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ApiApplication.Database.Entities
{
    public class MovieEntity
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ImdbId { get; set; }
        public string Stars { get; set; }
        public DateTime ReleaseDate { get; set; }
        [JsonIgnore]

        public List<ShowtimeEntity> Showtimes { get; set; }
    }
}
