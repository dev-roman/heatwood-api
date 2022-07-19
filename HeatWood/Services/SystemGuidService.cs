namespace HeatWood.Services;

public sealed class SystemGuidService : IGuidService
{
    public Guid NewGuid()
    {
        return Guid.NewGuid();
    }
}