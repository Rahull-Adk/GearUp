using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GearUp.Application.ServiceDtos.Car
{
    public class CarImageDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
