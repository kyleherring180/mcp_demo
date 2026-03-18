using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace McpDemo.Api.Telemetry;

public sealed class ProductMetrics : IDisposable
{
    public const string MeterName = "McpDemo.Api.Products";

    private readonly Meter _meter;

    // Counters
    private readonly Counter<long> _productSearches;
    private readonly Counter<long> _productViews;
    private readonly Counter<long> _productsCreated;
    private readonly Counter<long> _productsUpdated;
    private readonly Counter<long> _productsDeleted;

    // Histograms
    private readonly Histogram<int> _searchResultCount;

    public ProductMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _productSearches = _meter.CreateCounter<long>(
            "product.searches",
            unit: "{searches}",
            description: "Number of product search requests");

        _productViews = _meter.CreateCounter<long>(
            "product.views",
            unit: "{views}",
            description: "Number of individual product views");

        _productsCreated = _meter.CreateCounter<long>(
            "product.created",
            unit: "{products}",
            description: "Number of products created");

        _productsUpdated = _meter.CreateCounter<long>(
            "product.updated",
            unit: "{products}",
            description: "Number of products updated");

        _productsDeleted = _meter.CreateCounter<long>(
            "product.deleted",
            unit: "{products}",
            description: "Number of products deleted");

        _searchResultCount = _meter.CreateHistogram<int>(
            "product.search.results",
            unit: "{products}",
            description: "Number of results returned per search");
    }

    public void RecordSearch(int? categoryId, bool hasTextQuery, int resultCount)
    {
        var tags = new TagList
        {
            { "category_id", categoryId?.ToString() ?? "all" },
            { "has_text_query", hasTextQuery }
        };
        _productSearches.Add(1, tags);
        _searchResultCount.Record(resultCount, tags);
    }

    public void RecordProductView(int productId, string categoryName) =>
        _productViews.Add(1, new TagList { { "category", categoryName } });

    public void RecordProductCreated(string categoryName) =>
        _productsCreated.Add(1, new TagList { { "category", categoryName } });

    public void RecordProductUpdated() =>
        _productsUpdated.Add(1);

    public void RecordProductDeleted() =>
        _productsDeleted.Add(1);

    public void Dispose() => _meter.Dispose();
}
