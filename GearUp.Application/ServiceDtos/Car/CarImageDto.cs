using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GearUp.Domain.Enums;

namespace GearUp.Application.ServiceDtos.Car
{
    public class CarImageDto
    {
        public Guid Id { get; set; }
        public Guid CarId { get; set; }

        public string Url { get; set; } = string.Empty;
        public ImageProcessingStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
