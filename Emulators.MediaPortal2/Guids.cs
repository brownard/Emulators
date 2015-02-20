using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2
{
    static class Guids
    {
        public static readonly Guid WorkflowSatesMain = new Guid("3C370162-2586-416A-963C-F34DDDE184D4");
        public static readonly Guid WorkflowSatesEmulators = new Guid("97F9A2D9-B857-4890-9EF2-5E2BC66A53BB");

        public static readonly Guid ImporterModel = new Guid("C27FA8F9-500C-4AA3-83CB-93427ED1B2EA");
        public static readonly Guid ImporterState = new Guid("1782275A-4B3C-4E00-9F97-8B60DFD28F91");

        public static readonly Guid NewEmulatorWorkflow = new Guid("03F534A5-4495-4223-A7EB-0FC8D477CF4B");
        public static readonly Guid NewEmulatorState = new Guid("C69A5500-0059-4E4E-A331-2FD9835C4D0B");
        public static readonly Guid NewEmulatorConfigState = new Guid("2F9444A6-7F80-4A25-A680-859AC5861FB5");
                        
        public static readonly Guid ListDialogModel = new Guid("F140F32F-9F24-47D2-99B1-09A3A58D69FB");
        public static readonly Guid ProgressDialogModel = new Guid("A0E1D770-EBBB-4DD9-93FB-2D1CD901EDC4");
        public static readonly Guid TextInputDialogModel = new Guid("6410D85B-D8AC-456F-83F4-2F10057D424A");

        public static readonly Guid PlatformDetailsModel = new Guid("C89ED37E-8F20-486C-AD47-803366A57B00");
        public static readonly Guid PlatformDetailsState = new Guid("9EB96B6C-D94D-4C29-9CBF-E41E31BF2797");

        public static readonly Guid GameLauncherDialogModel = new Guid("B7450369-957B-429C-96B1-CA41CABAC8B4");
        public static readonly Guid GameLauncherDialogState = new Guid("5FCB7437-6A5B-42D3-B75B-51DD413BDA13");
    }
}
