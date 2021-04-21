﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Common.Models
{
    public class AwardMessage
    {
        public Guid Id { get; set; }
        public ulong OriginalMessageId { get; set; }

        // Discord message id in awards channel
        public ulong AwardedMessageId { get; set; }

        // Mainly for statistics reasons
        public ulong UserId { get; set; }
        public DateTime DateTime { get; set; }
        public uint AwardCount { get; set; }
    }
}
