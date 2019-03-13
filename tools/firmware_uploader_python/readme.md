# Note

This python based uploader was written before the latest details of the SGL firmware file format were known.

Only works with firmware version 3.1.2 because it uses hard coded offsets and the key value for that firmware.

On Windows...

On Windows,..
The driver the system installs for the GD-77, which is the HID driver, needs to be replaced by the LibUSB-win32 using Zadig (for USB device with idVendor=0x15a2, idProduct=0x0073)

### IMPORTANT... 
Once this driver is installed the CPS and official firmware loader will no longer work as they can't find the device
To use the CPS etc again, use the DeviceManager to uninstall the driver associated with idVendor=0x15a2, idProduct=0x0073 (this will appear as a libusb-win32 device)
Then unplug the GD-77 and reconnect, and the HID driver will be re-installed

This utility has only been tested on Windows but may work on Linux etc