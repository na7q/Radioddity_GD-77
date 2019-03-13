################################################################################################################################################
#
# GD-77 Firmware uploader. By Roger VK3KYY
#
# This script has only been tested on Windows, it may or may not work on Linux or OSX
#
# On Windows,..
# the driver the system installs for the GD-77, which is the HID driver, needs to be replaced by the LibUSB-win32 using Zadig
# for USB device with idVendor=0x15a2, idProduct=0x0073
# Once this driver is installed the CPS and official firmware loader will no longer work as they can't find the device
# To use the CPS etc again, use the DeviceManager to uninstall the driver associated with idVendor=0x15a2, idProduct=0x0073 (this will appear as a libusb-win32 device)
# Then unplug the GD-77 and reconnect, and the HID driver will be re-installed
#
################################################################################################################################################
import usb
import time
from array import array

# Globals
responseOK          =[0x03,0x00,0x01,0x00,0x41]




########################################################################
# Utilities to dump hex for testing
########################################################################
def hexdump(buf):
    i = 0
    cbuf = ""
    for b in buf:
        cbuf = cbuf + "0x%0.2X " % ord(b)
    return cbuf

def hexdumpArray(buf):
    i = 0
    cbuf = ""
    for b in buf:
        cbuf = cbuf + "0x%0.2X " % b
    return cbuf


########################################################################
# Send the data packet to the GD-77 and confirm the response is correct
########################################################################
def sendAndCheckResponse(dev,cmd,resp):
    USB_WRITE_ENDPOINT  = 0x02
    USB_READ_ENDPOINT   = 0x81
    TRANSFER_LENGTH     = 42
    zeroPad = [0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00]

    #pad out to the require 42 bytes length    
    if (len(cmd)<TRANSFER_LENGTH):
        cmd = cmd + zeroPad[0:TRANSFER_LENGTH-len(cmd)]
        
    if (len(resp)<TRANSFER_LENGTH):
        resp = resp + zeroPad[0:TRANSFER_LENGTH-len(resp)]

    ret = dev.write(USB_WRITE_ENDPOINT, cmd)
    ret = dev.read(USB_READ_ENDPOINT,TRANSFER_LENGTH,5000)
    expected = array("B", resp)
    if (expected == ret):
        return True
    else:
        print("Error read returned " + str(ret))
        return False

 
##############################
# Create checksum data packet
##############################
def createChecksumData(buf):
    #checksum data starts with a small header, followed by the 32 bit checksum value, least significant byte first
    
    checkSumData = [0x01,0x00,0x08,0x00,0x45,0x4e,0x44,0xff,0xDE,0xAD,0xBE,0xEF]        
    cs=0;
    for b in buf:
        cs = cs + ord(b) #the file data seems to be a string, hence the ord() function is needed to convert it to an integer
     
    checkSumData[8]     = cs%256
    checkSumData[9]     = int(cs>>8)%256
    checkSumData[10]    = int(cs>>16)%256
    checkSumData[11]    = int(cs>>24)%256
    return checkSumData

#####################################################
# Open firmware file on disk and sent it to the GD-77
#####################################################
def sendFileData(fileName,startAddress,dev):
    dataHeader  = [0x01,0x00,0x26,0x00,0xDE,0xAD,0xBE,0xEF,0x00,0x20]#Beginning of each data block trasmission. Values with DEADBEEF are where the 32 bit address is inserted. 
    BLOCK_LENGTH = 1024 #1k
    DATA_TRANSFER_SIZE = 0x20
    
    with open(fileName, 'rb') as f:
        input = f.read()
        
    fileLength = len(input)

    totalBlocks = (fileLength - startAddress) / BLOCK_LENGTH

    address = startAddress

    while address < fileLength:

        if ((address-startAddress) % BLOCK_LENGTH ==0):
            checksumStartAddress = address
 
        fileAddress = address - startAddress

        #Setup address of data in the header
        dataHeader[7] = (fileAddress) % 256
        dataHeader[6] = int(fileAddress >> 8) % 256
        dataHeader[5] = int(fileAddress >> 16) % 256
        dataHeader[4] = int(fileAddress >> 24) % 256      
            
        if (address + DATA_TRANSFER_SIZE < fileLength):
            dataBuffer = dataHeader + [ord(i) for i in input[address:(address+DATA_TRANSFER_SIZE)]]
            
            #print("D: "+ hexdumpArray(dataBuffer))
            if  (sendAndCheckResponse(dev,dataBuffer,responseOK) == False):
                print("Error sending data")
                return False
                break
            
            address = address + DATA_TRANSFER_SIZE
            if (((address -  startAddress) % 0x400) == 0):
                print("Sent block " + str((address -  startAddress)/BLOCK_LENGTH) + " of "+ str(totalBlocks))

                if (sendAndCheckResponse(dev,createChecksumData(input[checksumStartAddress:address]),responseOK) == False):
                    print("Error sending checksum")
                    return False
                    break
                    
        else:
            print("Sending last block")
            
            DATA_TRANSFER_SIZE = fileLength - address
            dataHeader[9]= DATA_TRANSFER_SIZE
            dataBuffer = dataHeader + [ord(i) for i in input[address:(address+DATA_TRANSFER_SIZE)]]
            #print("D: "+ hexdumpArray(dataBuffer))

            if (sendAndCheckResponse(dev,dataBuffer,responseOK) == False):
                print("Error sending data")
                return False
                break
            
            address = address + DATA_TRANSFER_SIZE
                 
            if (sendAndCheckResponse(dev,createChecksumData(input[checksumStartAddress:address]),responseOK) == False):
                print("Error sending checksum")
                return False
                break
    return True

###########################################################################################################################################
# Send commands to the GD-77 to verify we are the updater, prepare to program including erasing the internal program flash memory
###########################################################################################################################################
def sendInitialCommands(dev):
    commandLetterA      =[ 0x01,0x00,0x01,0x00,0x41] #A
    command0            =[[0x01,0x00,0x08,0x00,0x44,0x4f,0x57,0x4e,0x4c,0x4f,0x41,0x44],[0x03,0x00,0x08,0x00,0x23,0x55,0x50,0x44,0x41,0x54,0x45,0x3f]] # DOWNLOAD
    command1            =[commandLetterA,responseOK] 
    command2            =[[0x01,0x00,0x08,0x00,    0x44,0x56,0x30,0x31,0x65,0x6e,0x68,0x69],[0x03,0x00,0x04,0x00,0x44,0x56,0x30,0x31]] #.... DV01enhi (DV01enhi comes from deobfuscated sgl file)
    command3            =[[0x01,0x00,0x08,0x00,    0x46,0x2d,0x50,0x52,0x4f,0x47,0xff,0xff],responseOK] #... F-PROG..
    command4            =[[0x01,0x00,0x10,0x00,    0x53,0x47,0x2d,0x4d,0x44,0x2d,0x37,0x36,0x30,0xff,0xff,0xff,0xff,0xff,0xff,0xff],responseOK] #SG-MD-760
    command5            =[[0x01,0x00,0x08,0x00,    0x4d,0x44,0x2d,0x37,0x36,0x30,0xff,0xff],responseOK] #MD-760..
    command6            =[[0x01,0x00,0x08,0x00,    0x56,0x31,0x2e,0x30,0x30,0x2e,0x30,0x31],responseOK] #V1.00.01
    commandErase        =[[0x01,0x00,0x08,0x00,    0x46,0x2d,0x45,0x52,0x41,0x53,0x45,0xff],responseOK] #F-ERASE
    commandPostErase    =[commandLetterA,responseOK] 
    commandProgram      =[[0x01,0x00,0x08,0x00,0x50,0x52,0x4f,0x47,0x52,0x41,0x4d,0xf],responseOK]#PROGRAM
    commands            =[command0,command1,command2,command3,command4,command5,command6,commandErase,commandPostErase,commandProgram]
    commandNumber = 0
    # Send the commands which the GD-77 expects before the start of the data
    while commandNumber < len(commands):
        print("Sending command " + str(commandNumber));
        if sendAndCheckResponse(dev,commands[commandNumber][0],commands[commandNumber][1])==False:
            print("Error sending command")
            return False
            break
        commandNumber = commandNumber + 1
    return True

#####################################################
# Main function.
#####################################################
def main():
    dev = usb.core.find(idVendor=0x15a2, idProduct=0x0073)
    if (dev):
        
        dev.set_configuration()#seems to be needed for the usb to work !

        if (sendInitialCommands(dev) == True):
            if (sendFileData("GD-77_V3.1.2.sgl",0x041E,dev) == True):
                print("Firmware update complete. Please reboot the GD-77")
            else:
                print("Error while sending data")
        else:
            print("Error while sending initial commands")
       
        usb.util.dispose_resources(dev) #free up the USB device
        
    else:
        print("Cant find GD-77")


## Run the program
main() 
