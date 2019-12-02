using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.ViewModels;
using log4net;
using PoeShared;
using Prism.Modularity;
using Unity;

namespace EyeAuras.UI.Core.Services
{
    internal sealed class AuraRepository : IAuraRegistrator, IAuraRepository
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AuraRepository));

        private readonly IUnityContainer container;

        private readonly IDictionary<Type, Type> editorByModelType = new Dictionary<Type, Type>();
        private readonly ObservableCollection<IAuraModel> knownEntities = new ObservableCollection<IAuraModel>();
        private readonly IDictionary<Type, Type> modelTypeByAuraProperties = new Dictionary<Type, Type>();

        public AuraRepository(IUnityContainer container)
        {
            this.container = container;
            KnownEntities = new ReadOnlyObservableCollection<IAuraModel>(knownEntities);
        }
        
        public ReadOnlyObservableCollection<IAuraModel> KnownEntities { get; }

        public void Register<TAuraPropertiesEditor, TAuraModel>()
            where TAuraPropertiesEditor : IAuraPropertiesEditor<TAuraModel>
            where TAuraModel : IAuraModel
        {
            var auraModelType = typeof(TAuraModel);
            var editorType = typeof(TAuraPropertiesEditor);
            Log.Debug($"Registering Editor of type {auraModelType}, propertiesEditorType: {editorType}");

            var expectedEditorType = typeof(IAuraPropertiesEditor<>).MakeGenericType(auraModelType);

            if (!expectedEditorType.IsAssignableFrom(editorType))
            {
                throw new InvalidOperationException($"Properties editor type must be {expectedEditorType}, got {editorType}");
            }

            editorByModelType.Add(auraModelType, editorType);
        }

        public void Register<TAuraModel>() where TAuraModel : IAuraModel
        {
            var sample = CreateModel<TAuraModel>(typeof(TAuraModel));
            var propertiesType = GetPropertiesType(sample);
            Log.Debug($"Registering Model of type {typeof(TAuraModel)}, propertiesType: {propertiesType}");
            modelTypeByAuraProperties[propertiesType] = sample.GetType();
            knownEntities.Add(sample);
        }

        public TAuraBaseType CreateModel<TAuraBaseType>(Type auraModelType, IAuraContext context) where TAuraBaseType : IAuraModel
        {
            return (TAuraBaseType) container.Resolve(auraModelType);
        }

        public IAuraPropertiesEditor CreateEditor(IAuraModel model)
        {
            Guard.ArgumentNotNull(model, nameof(model));

            var propertiesType = model.GetType();
            if (editorByModelType.TryGetValue(propertiesType, out var editorType))
            {
                return (IAuraPropertiesEditor) container.Resolve(editorType);
            }

            Log.Warn($"Failed to resolve editor for property type {propertiesType}, source: {model} ({model.GetType()})");
            return null;
        }

        public TAuraBaseType CreateModel<TAuraBaseType>(IAuraProperties properties)
        {
            if (properties is EmptyAuraProperties)
            {
                throw new InvalidOperationException($"Unable to use {nameof(EmptyAuraProperties)} as properties for {typeof(TAuraBaseType)} creation - too generic");
            }

            var propertiesType = properties.GetType();
            if (!modelTypeByAuraProperties.TryGetValue(propertiesType, out var modelType))
            {
                Log.Warn($"Failed to resolve modelType for property type {propertiesType}, source: {properties}");
                if (typeof(IAuraTrigger).IsAssignableFrom(typeof(TAuraBaseType)))
                {
                    modelType = typeof(ProxyAuraTriggerViewModel);
                }
                else if (typeof(IAuraAction).IsAssignableFrom(typeof(TAuraBaseType)))
                {
                    modelType = typeof(ProxyAuraActionViewModel);
                }
                else
                {
                    modelType = typeof(ProxyAuraViewModel);
                }
            }

            var result = CreateModel<IAuraModel>(modelType);
            result.Properties = properties;
            return (TAuraBaseType) result;
        }

        private static Type GetPropertiesType(IAuraModel viewModel)
        {
            var expectedInterface = typeof(IAuraModel);
            var genericArgs = viewModel.GetType()
                .GetInterfaces()
                .Where(x => x.IsGenericType)
                .FirstOrDefault(x => expectedInterface.IsAssignableFrom(x.GetGenericTypeDefinition()));
            if (genericArgs == null)
            {
                throw new ModuleTypeLoadingException($"Failed to load settings of type {viewModel.GetType()} - interface {expectedInterface} was not found");
            }

            var configType = genericArgs.GetGenericArguments().First();
            return configType;
        }
    }
}