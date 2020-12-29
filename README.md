# USB OTG File Reader

![Logo](images/usb-and-folder-outline.svg)

USB OTG File Reader is an app for Android to copy files from USB devices
connected via an OTG cable.

## Build

Builds are created using AppCenter:
[![Build status](https://build.appcenter.ms/v0.1/apps/8afa61f9-838c-45a3-9a18-12704f9a9368/branches/main/badge)](https://appcenter.ms)

The latest build can be downloaded here:
https://install.appcenter.ms/users/michael.fink/apps/usbotgfilereader/distribution_groups/beta

## The Story

On modern Android versions, USB devices are automatically recognized and
appear in the Android's Files app (or even on Total Commander). Unfortunately
I have a device that's not recognized (an XC Tracer Vario device).

I searched around for Android apps that support USB devices, and I even found
a few that recognzied my device. Unfortunately all apps were ad-ridden or used
in-app purchases to sell you something you don't need.

After some searching I found a library called [libaums](https://github.com/magnusja/libaums)
(short for Android USB mass storage) and experimented a bit with a
Xamarin.Android bindings project. After a bit fiddling with the library and
USB permissions, I got it working and decided to build a Xamarin.Android app
around it.

And here it is, an Android app that does one thing, downloading files from an
USB device connected via a USB-OTG cable. If you're also searching for such an
app, maybe your search has an end! Try it out, it's free, has no ads and no
in-app purchases.

## Credits

The app uses some third-party pieces:

The app uses the [libaums](https://github.com/magnusja/libaums) library to
interact with USB mass storage devices connected to Android devices. The
library is licensed using the
[Apache License 2.0](https://github.com/magnusja/libaums/blob/develop/LICENSE).

The app uses icons from the Google Material icon set. The Google Material
Icons are licensed under the
[Apache License 2.0](https://github.com/google/material-design-icons/blob/master/LICENSE).

## License

The app is licensed under the [BSD 3-clause license](LICENSE.md).
