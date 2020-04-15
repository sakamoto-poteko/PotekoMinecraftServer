using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PotekoMinecraftServer.Models
{
    public class ResultResponse
    {
        public bool Result { get; set; }
        public string Message { get; set; }
    }
}
