namespace EyeAuras.Shared
{
    public interface IAuraRegistrator
    {
        void Register<TAuraModelEditor, TAuraModel>()
            where TAuraModelEditor : IAuraPropertiesEditor<TAuraModel>
            where TAuraModel : IAuraModel;

        void Register<TAuraModel>()
            where TAuraModel : IAuraModel;
    }
}