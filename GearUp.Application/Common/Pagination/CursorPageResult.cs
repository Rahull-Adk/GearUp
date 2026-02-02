namespace GearUp.Application.Common.Pagination
{
    public class CursorPageResult<T>
    {
            public IEnumerable<T> Items { get; set; } = [];
            public string? NextCursor { get; set; }
            public bool HasMore { get; set; }
    }
}