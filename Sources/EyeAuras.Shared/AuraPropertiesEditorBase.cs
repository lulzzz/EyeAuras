using System;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared
{
    public abstract class AuraPropertiesEditorBase<TAura> : DisposableReactiveObject, IAuraPropertiesEditor<TAura> where TAura : IAuraModel
    {
        private TAura currentValue;

        IAuraModel IAuraPropertiesEditor.Source
        {
            get => Source;
            set
            {
                if (value == null || value is TAura)
                {
                    Source = value == null
                        ? default
                        : (TAura) value;
                }
                else
                {
                    throw new InvalidOperationException($"Failed to assign value {value} (type: {value.GetType().Name}) to property of type {typeof(TAura)}");
                }
            }
        }

        public TAura Source
        {
            get => currentValue;
            set => RaiseAndSetIfChanged(ref currentValue, value);
        }
    }
}