﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using UCR.Models.Devices;
using UCR.Models.Mapping;
using UCR.Views.Controls;

namespace UCR.Models.Plugins
{
    public abstract class Plugin
    {
        // Persistence
        public string Title { get; set; }
        private Profile ParentProfile { get; set; }
        public List<DeviceBinding> Inputs { get; set; } // TODO Private
        public List<DeviceBinding> Outputs { get; set; } // TODO Private

        // Runtime


        public Plugin()
        {
        }

        public Plugin(Profile parentProfile) : base()
        {
            ParentProfile = parentProfile;
            Inputs = new List<DeviceBinding>();
            Outputs = new List<DeviceBinding>();
        }

        public bool Activate(UCRContext ctx)
        {
            bool success = true;
            success &= SubscribeInputs(ctx);
            return success;
        }

        public Device GetDevice(DeviceBinding deviceBinding)
        {
            switch (deviceBinding.DeviceBindingType)
            {
                case DeviceBindingType.Input:
                    return ParentProfile.GetInputDevice(deviceBinding);
                case DeviceBindingType.Output:
                    return ParentProfile.GetOutputDevice(deviceBinding);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected void WriteOutput(DeviceBinding output, long value)
        {
            if (output?.DeviceType == null) return;
            var device = ParentProfile.GetOutputDevice(output);
            device.WriteOutput(ParentProfile.ctx, output, value);
        }

        public virtual List<DeviceBinding> GetInputs()
        {
            return Inputs.Select(input => new DeviceBinding(input)).ToList();
        }

        private bool SubscribeInputs(UCRContext ctx)
        {
            bool success = true;
            foreach (var input in GetInputs())
            {
                if (input.DeviceType == null) continue;
                var device = ctx.ActiveProfile.GetInputDevice(input);
                if (device != null)
                {
                    // TODO test if switch is needed (type erasure?)
                    switch (input.DeviceType)
                    {
                        case DeviceType.Keyboard:
                            success &= ((Keyboard) device).SubscribeInput(input);
                            break;
                        case DeviceType.Mouse:
                            success &= ((Mouse)device).SubscribeInput(input);
                            break;
                        case DeviceType.Joystick:
                            success &= ((Joystick)device).SubscribeInput(input);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    success = false;
                }
            }
            return success;
        }

        protected DeviceBinding InitializeInputMapping(DeviceBinding.ValueChanged callbackFunc)
        {
            return InitializeMapping(DeviceBindingType.Input, callbackFunc);
        }

        protected DeviceBinding InitializeOutputMapping()
        {
            return InitializeMapping(DeviceBindingType.Output, null);
        }

        private DeviceBinding InitializeMapping(DeviceBindingType deviceBindingType, DeviceBinding.ValueChanged callbackFunc)
        {
            DeviceBinding deviceBinding = new DeviceBinding(callbackFunc, this, deviceBindingType);
            switch(deviceBindingType)
            {
                case DeviceBindingType.Input:
                    Inputs.Add(deviceBinding);
                    break;
                case DeviceBindingType.Output:
                    Outputs.Add(deviceBinding);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(deviceBindingType), deviceBindingType, null);
            }
            return deviceBinding;
        }
    }
}
