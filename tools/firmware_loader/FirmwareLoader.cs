//#define DUMP_USB_INFOS
//#define DUMP_IO
//#define EXTENDED_DEBUG
/*
 * 
 * Copyright (C)2019 Roger Clark. VK3KYY
 * 
 * Encryption sections based on work by Kai DG4KLU
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 *   1. Redistributions of source code must retain the above copyright notice,
 *      this list of conditions and the following disclaimer.
 *   2. Redistributions in binary form must reproduce the above copyright
 *      notice, this list of conditions and the following disclaimer in the
 *      documentation and/or other materials provided with the distribution.
 *   3. The name of the author may not be used to endorse or promote products
 *      derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR IMPLIED
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO
 * EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using System.Collections.ObjectModel;
using LibUsbDotNet.Main;
using System.IO;
using System.Windows.Forms;


namespace GD77_FirmwareLoader
{
	class FirmwareLoader
	{
		private static readonly byte[] responseOK = { 0x41 };
		private static readonly int VENDOR_ID = 0x15A2;
		private static readonly int PRODUCT_ID = 0x0073;
		private static int interfaceID = -1;
		public static UsbDevice _specifiedDevice = null;
		private static UsbEndpointReader _usbReader;
		private static UsbEndpointWriter _usbWriter;
		private static FrmProgress _progessForm;

		public static int UploadFirmare(string fileName, FrmProgress progessForm = null)
		{
			byte[] encodeKey = new Byte[4] { (0x61 + 0x00), (0x61 + 0x0C), (0x61 + 0x0D), (0x61 + 0x01) };
			_progessForm = progessForm;

			if (openUSBDevice() == false)
			{
				Console.WriteLine("Error. Can't connect to the GD-77");
				return -1;
			}

			byte[] fileBuf = File.ReadAllBytes(fileName);
			if (Path.GetExtension(fileName).ToLower() == ".sgl")
			{
				// Couls be a SGL file !
				fileBuf = checkForSGLAndReturnEncryptedData(fileBuf, encodeKey);
				if (fileBuf == null)
				{
					Console.WriteLine("Error. Missing SGL! in .sgl file header");
					closeUSBDevice();
					return -5;
				}
				Console.WriteLine("Firmware file confirmed as SGL");
			}
			else
			{
				Console.WriteLine("Firmware file is unencrypted binary");
				fileBuf = encrypt(fileBuf);
			}

			if (fileBuf.Length > 0x7b000)
			{
				Console.WriteLine("\nError. Firmware file too large.");
				closeUSBDevice();
				return -2;
			}

			_usbReader.Flush();
			_usbWriter.Flush();

			if (sendInitialCommands(encodeKey) == true)
			{
				int respCode = sendFileData(fileBuf);
				if (respCode == 0)
				{
					Console.WriteLine("\n *** Firmware update complete. Please reboot the GD-77 ***");
					if (_progessForm != null)
					{
						MessageBox.Show("Firmware update complete.Please reboot the GD-77.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
				else
				{
					switch (respCode)
					{
						case -1:
							Console.WriteLine("\nError. File to large");
							if (_progessForm != null)
							{
								MessageBox.Show("Error. File to large.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
							}
							break;
						case -2:
						case -3:
						case -4:
						case -5:
							Console.WriteLine("\nError " + respCode + " While sending data file");
							if (_progessForm != null)
							{
								MessageBox.Show("Error " + respCode + " While sending data file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
							}
							break;
					}
					closeUSBDevice();
					return -3;
				}
			}
			else
			{
				Console.WriteLine("\nError while sending initial commands");
				if (_progessForm != null)
				{
					MessageBox.Show("Error while sending initial commands.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				closeUSBDevice();
				return -4;
			}

			closeUSBDevice();
			return 0;
		}

		static private bool openUSBDevice()
		{
			UsbDeviceFinder usbFinder = new UsbDeviceFinder(VENDOR_ID, PRODUCT_ID);
			Byte configID = 255;

			// Find and open the usb device.
			UsbDevice usbDev = UsbDevice.OpenUsbDevice(usbFinder);

			try
			{
				// Find and open the usb device.
				usbDev = UsbDevice.OpenUsbDevice(usbFinder);

				// If the device is open and ready
				if (usbDev == null)
				{
					Console.WriteLine("Device Not Found.");
					if (_progessForm != null)
					{
						MessageBox.Show("Device Not Found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					return false;
				}

#if DUMP_USB_INFOS
                Console.WriteLine("*** GD77 USB Device Infos:\n  - " + usbDev.Info.ToString().Replace("\n", "\n  - "));
#endif
				for (int iConfig = 0; iConfig < usbDev.Configs.Count; iConfig++)
				{
					UsbConfigInfo configInfo = usbDev.Configs[iConfig];
#if DUMP_USB_INFOS
                    Console.WriteLine("   CONFIGURATION INFO: \n     - " + configInfo.ToString().Replace("\n", "\n     - "));
                    Console.WriteLine("   *** ConfigID: " + configInfo.Descriptor.ConfigID);
#endif
					configID = configInfo.Descriptor.ConfigID;

					ReadOnlyCollection<UsbInterfaceInfo> interfaceList = configInfo.InterfaceInfoList;
					for (int iInterface = 0; iInterface < interfaceList.Count; iInterface++)
					{
						UsbInterfaceInfo interfaceInfo = interfaceList[iInterface];
#if DUMP_USB_INFOS
                        Console.WriteLine("   INTERFACE INFO: \n     - " + interfaceInfo.ToString().Replace("\n", "\n     - "));
                        Console.WriteLine("   *** InterfaceID: " + interfaceInfo.Descriptor.InterfaceID);
#endif
						interfaceID = interfaceInfo.Descriptor.InterfaceID;

#if DUMP_USB_INFOS
                        ReadOnlyCollection<UsbEndpointInfo> endpointList = interfaceInfo.EndpointInfoList;
                        for (int iEndpoint = 0; iEndpoint < endpointList.Count; iEndpoint++)
                        {
                            Console.WriteLine("   ENDPOINT LIST: \n     - " + endpointList[iEndpoint].ToString().Replace("\n", "\n     - "));
                        }
#endif
					}
				}
#if DUMP_USB_INFOS
                Console.WriteLine("***\n");
#endif

				// If this is a "whole" usb device (libusb-win32, linux libusb)
				// it will have an IUsbDevice interface. If not (WinUSB) the 
				// variable will be null indicating this is an interface of a 
				// device.
				IUsbDevice wholeUsbDevice = usbDev as IUsbDevice;
				if (!ReferenceEquals(wholeUsbDevice, null))
				{
#if DUMP_USB_INFOS
                    Console.WriteLine("*** ConfigID: " + configID);
                    Console.WriteLine("*** InterfaceID: " + interfaceID);
#endif
					// This is a "whole" USB device. Before it can be used, 
					// the desired configuration and interface must be selected.

					// Select config #1
					wholeUsbDevice.SetConfiguration(configID);

					// Claim interface #0.
					wholeUsbDevice.ClaimInterface(interfaceID);
				}

				// open read endpoint 1.
				_usbReader = usbDev.OpenEndpointReader(ReadEndpointID.Ep01);
				_usbReader.ReadThreadPriority = ThreadPriority.AboveNormal;

				// open write endpoint 2
				_usbWriter = usbDev.OpenEndpointWriter(WriteEndpointID.Ep02);
			}
			catch (Exception ex)
			{
				Console.WriteLine("ERROR: " + ex.Message);
				if (_progessForm != null)
				{
					MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				return false;
			}

			_specifiedDevice = usbDev;

			return true;
		}

		static private void closeUSBDevice()
		{
			if (_specifiedDevice != null)
			{
				if (_specifiedDevice.IsOpen)
				{
					// If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
					// it exposes an IUsbDevice interface. If not (WinUSB) the 
					// 'wholeUsbDevice' variable will be null indicating this is 
					// an interface of a device; it does not require or support 
					// configuration and interface selection.
					IUsbDevice wholeUsbDevice = _specifiedDevice as IUsbDevice;
					if (!ReferenceEquals(wholeUsbDevice, null))
					{
						// Release interface #0.
						wholeUsbDevice.ReleaseInterface(interfaceID);
					}

					_specifiedDevice.Close();
				}
				_specifiedDevice = null;
			}
		}

		static private bool _sendAndReceiveData(byte[] cmd, byte[] resp)
		{
			ErrorCode ecWrite;
			ErrorCode ecRead;
			int transferredOut;
			int transferredIn;
			UsbTransfer usbWriteTransfer;
			UsbTransfer usbReadTransfer;
			byte[] readBuffer = new byte[4096];
			byte[] sendBuffer = new byte[4 + cmd.Length];

			// Prepare buffer
			sendBuffer[0] = 1;
			sendBuffer[1] = 0;
			sendBuffer[2] = Convert.ToByte(cmd.Length);
			sendBuffer[3] = Convert.ToByte(cmd.Length >> 8);

			Array.Copy(cmd, 0, sendBuffer, 4, cmd.Length);

			if (sendBuffer.Length < cmd.Length)
			{
				Console.WriteLine("ERROR: sendBuffer.Length < cmd.Length.");
				return false;
			}

			// Create and submit transfer
			ecRead = _usbReader.SubmitAsyncTransfer(readBuffer, 0, readBuffer.Length, 8000, out usbReadTransfer);
			if (ecRead != ErrorCode.None)
			{
				Console.WriteLine("ERROR: Submit Async Read Failed.");
				return false;
			}
			ecWrite = _usbWriter.SubmitAsyncTransfer(sendBuffer, 0, sendBuffer.Length, 8000, out usbWriteTransfer);
			if (ecWrite != ErrorCode.None)
			{
				Console.WriteLine("ERROR: Submit Async Write Failed.");
				return false;
			}

			WaitHandle.WaitAll(new WaitHandle[] { usbReadTransfer.AsyncWaitHandle/*, usbWriteTransfer.AsyncWaitHandle */}, 300, false);

			if (!usbReadTransfer.IsCompleted)
			{
				Console.Write(" [Zzz]");
				// Give it a bit of time to finish
				Thread.Sleep(5);
			}

			ecWrite = usbWriteTransfer.Wait(out transferredOut);
			ecRead = usbReadTransfer.Wait(out transferredIn);

			usbWriteTransfer.Dispose();
			usbReadTransfer.Dispose();

			if (transferredIn > 0)
			{
				Array.Copy(readBuffer, 4, resp, 0, Math.Min(resp.Length, transferredIn));
			}

			return true;
		}

		static bool sendAndCheckResponse(byte[] cmd, byte[] resp)
		{
			const int TRANSFER_LENGTH = 38;
			byte[] responsePadded = new byte[TRANSFER_LENGTH];
			byte[] recBuf = new byte[TRANSFER_LENGTH];

			if (resp.Length < TRANSFER_LENGTH)
			{
				Buffer.BlockCopy(resp, 0, responsePadded, 0, resp.Length);
			}

			_sendAndReceiveData(cmd, recBuf);

			if (recBuf.SequenceEqual(responsePadded))
			{
				return true;
			}
			else
			{
				Console.WriteLine();
				Console.WriteLine("Error unexpected response from GD-77: " + BitConverter.ToString(recBuf));
				if (_progessForm != null)
				{
					MessageBox.Show("Error unexpected response from GD-77: " + BitConverter.ToString(recBuf), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				return false;
			}
		}

		static private byte[] createChecksumData(byte[] buf, int startAddress, int endAddress)
		{
			//checksum data starts with a small header, followed by the 32 bit checksum value, least significant byte first
			byte[] checkSumData = { 0x45, 0x4e, 0x44, 0xff, 0xDE, 0xAD, 0xBE, 0xEF };
			int cs = 0;
			for (int i = startAddress; i < endAddress; i++)
			{
				cs = cs + (int)buf[i];
			}

			checkSumData[4] = (byte)(cs % 256);
			checkSumData[5] = (byte)((cs >> 8) % 256);
			checkSumData[6] = (byte)((cs >> 16) % 256);
			checkSumData[7] = (byte)((cs >> 24) % 256);

			return checkSumData;
		}

		static private void updateBlockAddressAndLength(byte[] buf, int address, int length)
		{
			// Length is 16 bits long in bytes 5 and 6
			buf[5] = (byte)((length) % 256);
			buf[4] = (byte)((length >> 8) % 256);

			// Address is 4 bytes long, in the first 4 bytes
			buf[3] = (byte)((address) % 256);
			buf[2] = (byte)((address >> 8) % 256);
			buf[1] = (byte)((address >> 16) % 256);
			buf[0] = (byte)((address >> 24) % 256);
		}

		static private int sendFileData(byte[] fileBuf)
		{
			byte[] dataHeader = new byte[0x20 + 0x06];
			const int BLOCK_LENGTH = 1024;
			int dataTransferSize = 0x20;
			int checksumStartAddress = 0;
			int address = 0;

			if (_progessForm != null)
			{
				_progessForm.SetLabel("Programming data");
			}

			int fileLength = fileBuf.Length;
			int totalBlocks = (fileLength / BLOCK_LENGTH) + 1;

#if EXTENDED_DEBUG
#else
			Console.WriteLine(" - Firmware uploading...");
#endif

			while (address < fileLength)
			{

				if (address % BLOCK_LENGTH == 0)
				{
					checksumStartAddress = address;
				}

				updateBlockAddressAndLength(dataHeader, address, dataTransferSize);

				if (address + dataTransferSize < fileLength)
				{
					Buffer.BlockCopy(fileBuf, address, dataHeader, 6, 32);

					if (sendAndCheckResponse(dataHeader, responseOK) == false)
					{
						Console.WriteLine("Error sending data");
						return -2;
					}

					address = address + dataTransferSize;
					if ((address % 0x400) == 0)
					{
						if (_progessForm != null)
						{
							_progessForm.SetProgressPercentage((address * 100 / BLOCK_LENGTH) / totalBlocks);
						}
#if EXTENDED_DEBUG
						Console.WriteLine("Sent block " + (address / BLOCK_LENGTH) + " of " + totalBlocks);
#else
						Console.Write(".");
#endif
						if (sendAndCheckResponse(createChecksumData(fileBuf, checksumStartAddress, address), responseOK) == false)
						{
							Console.WriteLine("Error sending checksum.");
							return -3;
						}
					}
				}
				else
				{
#if EXTENDED_DEBUG
					Console.WriteLine("Sending last block");
#else
					Console.Write(".");
#endif

					dataTransferSize = fileLength - address;
					updateBlockAddressAndLength(dataHeader, address, dataTransferSize);
					Buffer.BlockCopy(fileBuf, address, dataHeader, 6, dataTransferSize);

					if (sendAndCheckResponse(dataHeader, responseOK) == false)
					{
						Console.WriteLine("Error sending data.");
						return -4;
					}

					address = address + dataTransferSize;

					if (sendAndCheckResponse(createChecksumData(fileBuf, checksumStartAddress, address), responseOK) == false)
					{
						Console.WriteLine("Error sending checksum.");
						return -5;
					}
				}
			}
			return 0;
		}

		static private bool sendInitialCommands(byte[] encodeKey)
		{
			byte[] commandLetterA = new byte[] { 0x41 }; //A
			byte[][] command0 = new byte[][] { new byte[] { 0x44, 0x4f, 0x57, 0x4e, 0x4c, 0x4f, 0x41, 0x44 }, new byte[] { 0x23, 0x55, 0x50, 0x44, 0x41, 0x54, 0x45, 0x3f } }; // DOWNLOAD
			byte[][] command1 = new byte[][] { commandLetterA, responseOK };
			byte[][] command2 = new byte[][] { new byte[] { 0x44, 0x56, 0x30, 0x31, (0x61 + 0x00), (0x61 + 0x0C), (0x61 + 0x0D), (0x61 + 0x01) }, new byte[] { 0x44, 0x56, 0x30, 0x31 } }; //.... last 4 bytes of the command are the offset encoded as letters a - p (hard coded fr
			byte[][] command3 = new byte[][] { new byte[] { 0x46, 0x2d, 0x50, 0x52, 0x4f, 0x47, 0xff, 0xff }, responseOK }; //... F-PROG..
			byte[][] command4 = new byte[][] { new byte[] { 0x53, 0x47, 0x2d, 0x4d, 0x44, 0x2d, 0x37, 0x36, 0x30, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, responseOK }; //SG-MD-760
			byte[][] command5 = new byte[][] { new byte[] { 0x4d, 0x44, 0x2d, 0x37, 0x36, 0x30, 0xff, 0xff }, responseOK }; //MD-760..
			byte[][] command6 = new byte[][] { new byte[] { 0x56, 0x31, 0x2e, 0x30, 0x30, 0x2e, 0x30, 0x31 }, responseOK }; //V1.00.01
			byte[][] commandErase = new byte[][] { new byte[] { 0x46, 0x2d, 0x45, 0x52, 0x41, 0x53, 0x45, 0xff }, responseOK }; //F-ERASE
			byte[][] commandPostErase = new byte[][] { commandLetterA, responseOK };
			byte[][] commandProgram = { new byte[] { 0x50, 0x52, 0x4f, 0x47, 0x52, 0x41, 0x4d, 0xf }, responseOK };//PROGRAM
			byte[][][] commands = { command0, command1, command2, command3, command4, command5, command6, commandErase, commandPostErase, commandProgram };
			string[] commandNames = {"Sending Download command", "Sending ACK", "Sending encryption key", "Sending F-PROG command", "Sending radio modem number",
				"Sending radio modem number 2", "Sending version", "Sending erase command", "Send post erase command", "Sending Program command"};
			int commandNumber = 0;

			Buffer.BlockCopy(encodeKey, 0, command2[0], 4, 4);

			// Send the commands which the GD-77 expects before the start of the data
			while (commandNumber < commands.Length)
			{
				if (_progessForm != null)
				{
					_progessForm.SetLabel(commandNames[commandNumber]);
				}

#if EXTENDED_DEBUG
				Console.WriteLine("Sending command " + commandNames[commandNumber] + " [ " + commandNumber + " ]");
#else
				//Console.Write(".");
				Console.Write("\n - " + commandNames[commandNumber]);
#endif

				if (sendAndCheckResponse(commands[commandNumber][0], commands[commandNumber][1]) == false)
				{
					Console.WriteLine("Error sending command.");
					return false;
				}
				commandNumber = commandNumber + 1;
			}
#if EXTENDED_DEBUG
#else
			Console.WriteLine();
#endif
			return true;
		}

		static byte[] encrypt(byte[] unencrypted)
		{
			int shift = 0x0807;
			byte[] encrypted = new byte[unencrypted.Length];
			int data;

			byte[] encryptionTable = new byte[32768];
			int len = unencrypted.Length;
			for (int address = 0; address < len; address++)
			{
				data = unencrypted[address] ^ DataTable.EncryptionTable[shift++];

				data = ~(((data >> 3) & 0x1F) | ((data << 5) & 0xE0)); // 0x1F is 0b00011111   0xE0 is 0b11100000

				encrypted[address] = (byte)data;

				if (shift >= 0x7fff)
				{
					shift = 0;
				}
			}
			return encrypted;
		}



		static byte[] checkForSGLAndReturnEncryptedData(byte[] fileBuf, byte[] encodeKey)
		{
			byte[] header_tag = new byte[] { (byte)'S', (byte)'G', (byte)'L', (byte)'!' };

			// read header tag
			byte[] buf_in_4 = new byte[4];
			Buffer.BlockCopy(fileBuf, 0, buf_in_4, 0, buf_in_4.Length);

			if (buf_in_4.SequenceEqual(header_tag))
			{
				// read and decode offset and xor tag
				//stream_in.Seek(0x000C, SeekOrigin.Begin);
				//stream_in.Read(buf_in_4, 0, buf_in_4.Length);
				Buffer.BlockCopy(fileBuf, 0x000C, buf_in_4, 0, buf_in_4.Length);
				for (int i = 0; i < buf_in_4.Length; i++)
				{
					buf_in_4[i] = (byte)(buf_in_4[i] ^ header_tag[i]);
				}
				int offset = buf_in_4[0] + 256 * buf_in_4[1];
				byte[] xor_data = new byte[] { buf_in_4[2], buf_in_4[3] };

				// read and decode part of the header
				byte[] buf_in_512 = new byte[512];
				//stream_in.Seek(offset + 0x0006, SeekOrigin.Begin);
				//stream_in.Read(buf_in_512, 0, buf_in_512.Length);
				Buffer.BlockCopy(fileBuf, offset + 0x0006, buf_in_512, 0, buf_in_512.Length);
				int xor_idx = 0;
				for (int i = 0; i < buf_in_512.Length; i++)
				{
					buf_in_512[i] = (byte)(buf_in_512[i] ^ xor_data[xor_idx]);
					xor_idx++;
					if (xor_idx == 2)
					{
						xor_idx = 0;
					}
				}

				// dump decoded part of the header
				/*
				Console.WriteLine(String.Format("Offset  : {0:X4}", offset));
				Console.WriteLine(String.Format("XOR-Data: {0:X2}{1:X2}", xor_data[0], xor_data[1]));
				int pos = 0;
				int idx = 0;
				string line1 = "";
				string line2 = "";
				for (int i = 0; i < buf_in_512.Length; i++)
				{
					if (line1 == "")
					{
						line1 = String.Format("{0:X6}: ", i);
					}
					line1 = line1 + String.Format(" {0:X2}", buf_in_512[idx]);
					if ((buf_in_512[idx] >= 0x20) && (buf_in_512[idx] < 0x7f))
					{
						line2 = line2 + (char)buf_in_512[idx];
					}
					else
					{
						line2 = line2 + ".";
					}
					idx++;
					pos++;

					if (pos == 16)
					{
						Console.WriteLine(line1 + " " + line2);
						line1 = "";
						line2 = "";
						pos = 0;
					}
				}
				*/

				// extract encoding key

				byte key1 = (byte)(buf_in_512[0x005D] - 'a');
				byte key2 = (byte)(buf_in_512[0x005E] - 'a');
				byte key3 = (byte)(buf_in_512[0x005F] - 'a');
				byte key4 = (byte)(buf_in_512[0x0060] - 'a');
				int encoding_key = (key1 << 12) + (key2 << 8) + (key3 << 4) + key4;

				Buffer.BlockCopy(buf_in_512, 0x005D, encodeKey, 0, 4);


				// extract length
				byte length1 = (byte)buf_in_512[0x0000];
				byte length2 = (byte)buf_in_512[0x0001];
				byte length3 = (byte)buf_in_512[0x0002];
				byte length4 = (byte)buf_in_512[0x0003];
				int length = (length4 << 24) + (length3 << 16) + (length2 << 8) + length1;

				// extract encoded raw firmware
				/*FileStream stream_out = new FileStream(args[0] + "_" + String.Format("{0:X4}", encoding_key) + "_" + String.Format("{0:X6}", length) + ".raw", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
				stream_in.Seek(stream_in.Length - length, SeekOrigin.Begin);
				int c;
				while ((c = stream_in.ReadByte())>=0)
				{
					stream_out.WriteByte((byte)c);
				}*/

				byte[] retBuf = new byte[length];
				Buffer.BlockCopy(fileBuf, fileBuf.Length - length, retBuf, 0, retBuf.Length);
				return retBuf;
			}
			else
			{
				Console.WriteLine("ERROR: SGL! header missing.");
				return null;
			}
		}

	}
}
