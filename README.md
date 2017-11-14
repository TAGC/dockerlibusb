## Overview

A small project put together to investigate why applications running inside a Docker container are not able to reconnect to USB devices after they've been disconnected.

Refer to [This Moby issue](https://github.com/moby/moby/issues/35359#issuecomment-344204870) for more details.

## Setting up

This application is built on .NET Core 2. While it can be launched on Windows, it will fail with an exception pretty early on as this project is only designed to test USB disconnect/reconnection behaviour under a modern Linux environment (one that enumerates USB devices under `/dev/bus/usb`.)

This application also depends upon `libusb-1.0.dll` via its dependency on `LibUsbDotNet`. On a Linux host using apt package manager, you can install this dependency by running:

```
$ apt-get update
$ apt-get install -y libusb-1.0-0-dev usbutils
```

It may be necessary to symlink the actual library file for the application to run. For example, on a Rasperry Pi this can be done by cd'ing into the directory with the application binaries and running:

```
$ ln -s /lib/arm-linux-gnueabihf/libusb-1.0.so.0 libusb-1.0.dll
```

The host machine will need to have Docker installed. The Dockerfile in this repo builds an image that's designed to run on a system with an ARM architecture - in particular a Raspberry Pi 3 Model B. This can be adapted to work for other devices.

Finally, a USB device is needed to test against.

## Exposing the issue

To expose the actual issue, this application should be run in two different ways:

 - Natively on the Linux machine
 - Within a Docker container running on the Linux machine

In my case, I've tested this using a Raspberry Pi 3 Model B. These are the steps I took in each case:

#### Testing as a native application

1. Open `DockerLibUsb` (the *project* folder, not the solution folder) within a terminal.

2. Run `dotnet -c Release -r linux-arm -o dockerlibusb` to produce a self-contained version of the application under `DockerLibUsb/dockerlibusb`.

3. Copy `dockerlibusb` over SSH to the Raspberry Pi under the home directory.

4. SSH into the Raspberry Pi, cd into `~/dockerlibusb`.

5. Run `ln -s /lib/arm-linux-gnueabihf/libusb-1.0.so.0 libusb-1.0.dll` to symlink libusb-1.0.dll.

6. Run `chmod +x DockerLibUsb` to make the application binary executable.

7. Connect the test USB device (with known VID and PID) to the Raspberry Pi. If the VID and PID are not known, they can be discovered using `lsusb`.

8. Run `sudo ./DockerLibUsb` to run the application, entering the VID and PID of the connected USB device.

9. Confirm that USB communication successfully establishes with the device.

10. Disconnect the USB device from the Raspberry Pi - this should be picked up by the application and it should terminate communication.

11. Reconnect the USB device to the Raspberry Pi - this should also be picked up, and communication successfully re-established with the device.

12. Repeat steps 10 and 11 until satisfied no issue exists.

##### Example

```
$ sudo ./DockerLibUsb
While running this program, try unplugging & replugging the test USB device.
Monitor the console logs to check whether communication is being re-established
Vendor ID not specified in environment. Please enter it (0-65535): <redacted>
Product ID not specified in environment. Please enter it (0-65535): 1
Using USB device with VID=<redacted> and PID=1 as test subject
[14:03:52 DBG] Populating connected USB devices under /dev/bus/usb
[14:03:53 VRB] Discovered USB device (VID: <redacted>, PID: 1)
[14:03:53 VRB] Discovered USB device (VID: 6551, PID: 9267)
[14:03:53 VRB] Discovered USB device (VID: 9354, PID: -31386)
[14:03:53 VRB] Discovered USB device (VID: 1060, PID: -5120)
[14:03:53 VRB] Discovered USB device (VID: 1060, PID: -27372)
[14:03:53 VRB] Discovered USB device (VID: 7531, PID: 2)
[14:03:53 DBG] Attempting to start/restart communication.
[14:03:54 INF] Successfully initiated communication with TestDevice
Press any key to end the test...
[14:03:57 VRB] USB device removed (VID: <redacted>, PID: 1)
[14:03:57 DBG] USB event concerning device occurred
[14:03:57 INF] Resetting TestDevice as USB device
[14:03:57 INF] Terminated communication with TestDevice
[14:03:59 VRB] USB device connected (VID: <redacted>, PID: 1)
[14:03:59 DBG] USB event concerning device occurred
[14:04:00 DBG] Attempting to start/restart communication.
[14:04:00 ERR] Cannot find TestDevice
[14:04:00 ERR] Failed to start/restart communication.
System.InvalidOperationException: Cannot find TestDevice
   at DockerLibUsb.Communication.TestUsbCommunicationService.<Start>d__12.MoveNext() in C:\Users\<redacted>\source\repos\DockerLibUsb\DockerLibUsb\Communication\TestUsbCommunicationService.cs:line 71
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at DockerLibUsb.Communication.RestartableCommunicationService.<TryRestartCommunication>d__24.MoveNext() in C:\Users\<redacted>\source\repos\DockerLibUsb\DockerLibUsb\Communication\RestartableCommunicationService.cs:line 193
[14:04:01 DBG] Attempting to start/restart communication.
[14:04:01 INF] Successfully initiated communication with TestDevice
[14:04:05 VRB] USB device removed (VID: <redacted>, PID: 1)
[14:04:05 DBG] USB event concerning device occurred
[14:04:05 INF] Resetting TestDevice as USB device
[14:04:05 INF] Terminated communication with TestDevice
[14:04:07 VRB] USB device connected (VID: <redacted>, PID: 1)
[14:04:07 DBG] USB event concerning device occurred
[14:04:07 DBG] Attempting to start/restart communication.
[14:04:07 ERR] Cannot find TestDevice
[14:04:07 ERR] Failed to start/restart communication.
System.InvalidOperationException: Cannot find TestDevice
   at DockerLibUsb.Communication.TestUsbCommunicationService.<Start>d__12.MoveNext() in C:\Users\<redacted>\source\repos\DockerLibUsb\DockerLibUsb\Communication\TestUsbCommunicationService.cs:line 71
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at DockerLibUsb.Communication.RestartableCommunicationService.<TryRestartCommunication>d__24.MoveNext() in C:\Users\<redacted>\source\repos\DockerLibUsb\DockerLibUsb\Communication\RestartableCommunicationService.cs:line 193
[14:04:08 DBG] Attempting to start/restart communication.
[14:04:08 INF] Successfully initiated communication with TestDevice

[14:04:12 DBG] Disposing restartable USB communication service
[14:04:12 INF] Resetting TestDevice as USB device
[14:04:13 INF] Terminated communication with TestDevice
```

#### Testing as a Dockerised application

1. Build a Docker image containing the application with `docker build . -t dockerlibusb`. For a Raspberry Pi 3 Model B, the Dockerfile located in the solution folder should work as is. Otherwise this can be adapted.

2. Run `docker save -o dockerlibusb.tar` to save the Docker image to a file.

3. Copy `dockerlibusb.tar` over SSH to the Raspberry Pi under the home directory.

4. SSH into the Raspberry Pi, cd into `~` if not already there.

5. Run `docker load -i dockerlibusb.tar` to load the Docker image.

6. Connect the test USB device (with known VID and PID) to the Raspberry Pi. If the VID and PID are not known, they can be discovered using `lsusb`.

7. Run `docker run --it --rm --privileged -v /dev/bus/usb:/dev/bus/usb dockerlibusb` to launch the application within a Docker container. Enter the VID and PID of the connected USB device when prompted. Alternatively, run the docker command with `-e TEST_VID=<<VID>> -e TEST_PID=<<PID>>` to pass the VID and PID of the test device as environment variables.

8. Confirm that USB communication successfully establishes with the device.

9. Disconnect the USB device from the Raspberry Pi - this should be picked up by the application and it should terminate communication.

10. Reconnect the USB device to the Raspberry Pi - this should also be picked up, but communication with the device should fail to re-establish.

##### Example

```
$ docker run -it --rm --privileged -v /dev/bus/usb:/dev/bus/usb -e TEST_VID=<redacted> -e TEST_PID=1 dockerlibusb
While running this program, try unplugging & replugging the test USB device.
Monitor the console logs to check whether communication is being re-established
Using USB device with VID=<redacted> and PID=1 as test subject
[12:53:56 DBG] Populating connected USB devices under /dev/bus/usb
[12:53:56 VRB] Discovered USB device (VID: <redacted>, PID: 1)
[12:53:57 VRB] Discovered USB device (VID: 6551, PID: 9267)
[12:53:57 VRB] Discovered USB device (VID: 9354, PID: -31386)
[12:53:57 VRB] Discovered USB device (VID: 1060, PID: -5120)
[12:53:57 VRB] Discovered USB device (VID: 1060, PID: -27372)
[12:53:57 VRB] Discovered USB device (VID: 7531, PID: 2)
[12:53:57 DBG] Attempting to start/restart communication.
[12:53:58 INF] Successfully initiated communication with TestDevice
Press any key to end the test...
[12:54:01 VRB] USB device removed (VID: <redacted>, PID: 1)
[12:54:01 DBG] USB event concerning device occurred
[12:54:01 INF] Resetting TestDevice as USB device
[12:54:01 INF] Terminated communication with TestDevice
[12:54:04 VRB] USB device connected (VID: <redacted>, PID: 1)
[12:54:04 DBG] USB event concerning device occurred
[12:54:04 DBG] Attempting to start/restart communication.
[12:54:04 ERR] Cannot find TestDevice
[12:54:04 ERR] Failed to start/restart communication.
System.InvalidOperationException: Cannot find TestDevice
   at DockerLibUsb.Communication.TestUsbCommunicationService.<Start>d__12.MoveNext() in /app/DockerLibUsb/Communication/TestUsbCommunicationService.cs:line 71
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at DockerLibUsb.Communication.RestartableCommunicationService.<TryRestartCommunication>d__24.MoveNext() in /app/DockerLibUsb/Communication/RestartableCommunicationService.cs:line 193
[12:54:05 DBG] Attempting to start/restart communication.
[12:54:05 ERR] Cannot find TestDevice
[12:54:05 ERR] Failed to start/restart communication.
System.InvalidOperationException: Cannot find TestDevice
   at DockerLibUsb.Communication.TestUsbCommunicationService.<Start>d__12.MoveNext() in /app/DockerLibUsb/Communication/TestUsbCommunicationService.cs:line 71
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at DockerLibUsb.Communication.RestartableCommunicationService.<TryRestartCommunication>d__24.MoveNext() in /app/DockerLibUsb/Communication/RestartableCommunicationService.cs:line 193
[12:54:05 DBG] Attempting to start/restart communication.
[12:54:05 ERR] Cannot find TestDevice
[12:54:06 ERR] Failed to start/restart communication.
System.InvalidOperationException: Cannot find TestDevice
   at DockerLibUsb.Communication.TestUsbCommunicationService.<Start>d__12.MoveNext() in /app/DockerLibUsb/Communication/TestUsbCommunicationService.cs:line 71
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at DockerLibUsb.Communication.RestartableCommunicationService.<TryRestartCommunication>d__24.MoveNext() in /app/DockerLibUsb/Communication/RestartableCommunicationService.cs:line 193
[12:54:07 DBG] Attempting to start/restart communication.
[12:54:07 ERR] Cannot find TestDevice
[12:54:07 ERR] Failed to start/restart communication.
System.InvalidOperationException: Cannot find TestDevice
   at DockerLibUsb.Communication.TestUsbCommunicationService.<Start>d__12.MoveNext() in /app/DockerLibUsb/Communication/TestUsbCommunicationService.cs:line 71
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at DockerLibUsb.Communication.RestartableCommunicationService.<TryRestartCommunication>d__24.MoveNext() in /app/DockerLibUsb/Communication/RestartableCommunicationService.cs:line 193


[12:54:19 DBG] Disposing restartable USB communication service
```