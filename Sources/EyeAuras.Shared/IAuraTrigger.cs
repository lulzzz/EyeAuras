namespace EyeAuras.Shared
{
    public interface IAuraTrigger : IAuraModel
    {
        string TriggerName { get; }

        string TriggerDescription { get; }

        bool IsActive { get; }
    }
}