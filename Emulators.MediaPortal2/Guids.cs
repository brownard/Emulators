﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2
{
    static class Guids
    {
        public static readonly Guid StartupState = new Guid("F1F446EC-03FD-4A22-BA04-7C45E6D72E06");

        public static readonly Guid WorkflowSatesMain = new Guid("3C370162-2586-416A-963C-F34DDDE184D4");
        public static readonly Guid WorkflowSatesEmulators = new Guid("97F9A2D9-B857-4890-9EF2-5E2BC66A53BB");

        public static readonly Guid NewEmulatorWorkflow = new Guid("03F534A5-4495-4223-A7EB-0FC8D477CF4B");
        public static readonly Guid NewEmulatorState = new Guid("C69A5500-0059-4E4E-A331-2FD9835C4D0B");
        public static readonly Guid NewEmulatorConfigState = new Guid("2F9444A6-7F80-4A25-A680-859AC5861FB5");

        public static readonly Guid ListDialogWorkflow = new Guid("F140F32F-9F24-47D2-99B1-09A3A58D69FB");
        
        public static readonly Guid LaunchGameDialogWorkflow = new Guid("2471F9F2-CAE1-4AEE-96BD-6647FF191F6E");
        public static readonly Guid LaunchGameDialogState = new Guid("95639608-6CCC-4604-921C-AC34C4BFEFC5");
    }
}
