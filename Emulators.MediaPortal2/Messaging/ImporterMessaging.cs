using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Messaging
{
    class ImporterMessaging
    {
        public const string Channel = "EmulatorsImporterMessagingChannel";
        public enum MessageType
        {
            Init
        }

        public static void SendImporterMessage(MessageType messageType)
        {
            SystemMessage msg = new SystemMessage(messageType);
            ServiceRegistration.Get<IMessageBroker>().Send(Channel, msg);
        }
    }
}
