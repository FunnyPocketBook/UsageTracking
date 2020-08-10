using System;
using System.Collections.Generic;
using System.Text;
using CSCore.CoreAudioAPI;

namespace ProgramTracker.Helper
{
    class AudioDetector
    {
        public static MMDevice GetDefaultRenderDevice()
        {
            using var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        }

        public static bool IsAudioPlaying(MMDevice device)
        {
            using var meter = AudioMeterInformation.FromDevice(device);
            return meter.PeakValue > 0;
        }
        
        public static bool IsAnyAudioPlaying()
        {
            //todo: implement goddamn SAMples!!!!!!!
            using var enumerator = new MMDeviceEnumerator();
            foreach (MMDevice device in enumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active))
            {
                using var meter = AudioMeterInformation.FromDevice(device);
                if (meter.PeakValue > 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
