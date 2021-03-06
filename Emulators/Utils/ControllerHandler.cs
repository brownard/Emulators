﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DirectX.DirectInput;

namespace Emulators
{
    public class ControllerHandler
    {
        public static bool CheckControllerState()
        {
            DeviceList devices = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly);
            return devices.Count > 0;
        }
    }
}
