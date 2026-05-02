using CastoriceHoney.Server.Data.Config.Task;

namespace CastoriceHoney.Server.Data.Config;

public class FetchAdvPropData
{
    public DynamicFloat GroupID { get; set; } = new();
    public DynamicFloat ID { get; set; } = new();
}