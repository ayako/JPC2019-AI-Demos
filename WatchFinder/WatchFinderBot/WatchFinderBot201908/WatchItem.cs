using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace WatchFinderBot
{
    [SerializePropertyNamesAsCamelCase]
    public class WatchProduct
    {
        public int SearchScore { get; set; }

        [Key]
        [IsSearchable, IsSortable]
        public string ProductId { get; set; }

        [IsSearchable]
        public string ProductName { get; set; }

        [IsSearchable]
        public string ProductImage { get; set; }

        [IsSearchable]
        public string ProductUrl { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string Released { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string Collection { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string Movement { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string Packaging { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string WaterResistance { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string Color { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string StrapColor { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string StrapMaterial { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string ClaspMaterial { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string ClaspType { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string DialColor { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string CaseColor { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string CaseShape { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string CaseMaterial { get; set; }

        [IsSearchable]
        public string Warranty { get; set; }

        public ProductDesc ProductDesc { get; set; }

        public CaseDimensions CaseDimensions { get; set; }
    }

    public class ProductDesc
    {
        [IsSearchable]
        public string ja { get; set; }

        [IsSearchable]
        public string en { get; set; }
    }

    public class CaseDimensions
    {
        [IsSearchable]
        public float width { get; set; }

        [IsSearchable]
        public float thickness { get; set; }

        [IsSearchable]
        public float height { get; set; }
    }

    public class WatchItem
    {
        [Key]
        [IsSearchable, IsSortable]
        public string ItemID { get; set; }

        [IsSearchable, IsSortable]
        public string ItemName { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string Collection { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string Image { get; set; }

        [IsSearchable]
        [Analyzer(AnalyzerName.AsString.JaLucene)]
        public string Description_ja { get; set; }

        [IsSearchable]
        [Analyzer(AnalyzerName.AsString.EnLucene)]
        public string Description_en { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable]
        public string Color { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable]
        public string Band { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable]
        public string Size { get; set; }

        [IsSortable, IsSearchable]
        public string Price { get; set; }

        public string Photo { get; set; }

        public string URL { get; set; }
    }
}
