﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoardGame.Infrastructure.Data.Interfaces;

namespace BoardGame.Infrastructure.Data.DTO
{
    public class MoveDto : IDto
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int PlayerId { get; set; }
        //public DateTime Date { get; set; }
    }
}
