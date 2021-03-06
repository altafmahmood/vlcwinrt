﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2+ and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VLC_WINRT.Common;
using VLC_WINRT.ViewModels;

namespace VLC_WINRT.Utility.Commands.VideoPlayer
{
    public class SetAudioTrackCommand : AlwaysExecutableCommand
    {
        public override async void Execute(object parameter)
        {
            await Locator.PlayVideoVM.SetAudioTrack((int)parameter);
        }
    }
}
