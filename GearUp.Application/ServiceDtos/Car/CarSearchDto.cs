namespace GearUp.Application.ServiceDtos.Car
{
    public class CarSearchDto
    {
        public string? Query { get; set; }
        public string? Color { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public string? SortBy { get; set; } 
        public string? SortOrder { get; set; }
        public int Page { get; set; } = 1;
    }

}

