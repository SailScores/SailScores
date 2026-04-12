using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SailScores.Api.Dtos.Public
{
    public class PublicListResponseDto<T>
    {
        public IList<T> Items { get; set; } = new List<T>();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PublicPaginationDto Pagination { get; set; }
    }
}
