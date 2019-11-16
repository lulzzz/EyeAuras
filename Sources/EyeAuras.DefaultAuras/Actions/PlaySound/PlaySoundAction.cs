using System.Windows;
using EyeAuras.Shared;
using log4net;
using PoeShared.Audio.Services;

namespace EyeAuras.DefaultAuras.Actions.PlaySound
{
    internal sealed class PlaySoundAction : AuraActionBase<PlaySoundActionProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PlaySoundAction));

        private readonly IAudioNotificationsManager notificationsManager;
        private string notification;

        public PlaySoundAction(IAudioNotificationsManager notificationsManager)
        {
            this.notificationsManager = notificationsManager;
            Notification = AudioNotificationType.Ping.ToString();
        }

        public string Notification
        {
            get => notification;
            set => this.RaiseAndSetIfChanged(ref notification, value);
        }

        protected override void Load(PlaySoundActionProperties source)
        {
            Notification = source.Notification;
        }

        protected override PlaySoundActionProperties Save()
        {
            return new PlaySoundActionProperties()
            {
                Notification = notification
            };
        }

        public override string ActionName { get; } = "Play Sound";
        
        public override string ActionDescription { get; } = "plays specified sound";
        
        public override void Execute()
        {
            if (string.IsNullOrEmpty(notification))
            {
                return;
            }
            Log.Debug($"Playing notification {notification}");
            notificationsManager.PlayNotification(notification);
        }
    }
}