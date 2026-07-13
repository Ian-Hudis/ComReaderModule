# ComReaderModule

A simple program for Reading USB com port data without occupying the com port. Program uses USBPcap.exe. Will later make this into a library to use for other projects.
To Run the application , you will need to have USBPcap installed and running on your system. You can download it from the official website: https://desowin.org/usbpcap/

The COM Reader Module is a software utility designed to monitor USB COM port communication without assuming control of the port. Its primary function is to capture incoming communication data and preserve that information for subsequent review, troubleshooting, or verification. The application requires both USBPcap and the appropriate .NET runtime to be installed to operate as intended.
Software Function: The COM Reader Module monitors USB COM port traffic records the communication data detected during operation, and saves that data for troubleshooting, process verification, or later analysis. The application performs this function without taking control of the COM port itself.


Installation Procedure:
1.	Copy the .NET runtime installer and the USBPcap setup file into the Downloads folder.
   <img width="884" height="249" alt="image" src="https://github.com/user-attachments/assets/52989369-4651-4ae0-8563-6ab83f182330" />
3.	Install both applications by following the on-screen prompts. Administrator privileges are required to complete the installation.
4.	During the USBPcap installation, note where the USBcap.exe file location and USB identifier value if it is changed from the default setting. By default, the default location will be “C:\Program Files\USBPcap\USBPcapCMD.exe” and the USB identifier will be “USBPcap1”.  If these values are changed, you will have to modify the “Config.ini” file in the “USBComPortReader” folder.
<img width="975" height="223" alt="image" src="https://github.com/user-attachments/assets/09ee2eec-2a44-4f64-af97-de4240389a41" />
5.	Restart the computer.


Software Startup Procedure:
1.	Open the folder titled “USBComPortReader.”
   <img width="975" height="239" alt="image" src="https://github.com/user-attachments/assets/943fcf0d-4cb8-41fa-bb33-2548b1a3dace" />
3.	Launch the application by selecting ComReaderModule.exe.
   <img width="975" height="453" alt="image" src="https://github.com/user-attachments/assets/7ad10ea1-c311-4000-b34e-e66687307c97" />
5.	When prompted, enter the production username and password to grant administrator access.
6.	Once the application window is displayed, the software is active and monitoring COM port data.
<img width="975" height="230" alt="image" src="https://github.com/user-attachments/assets/a12a3735-9567-4711-8ebb-10a33dfbcd4e" />


Data Storage Location:
Captured data is stored in the Data folder located within the COM Reader Module directory. The application saves this information as text (.txt) files. A new text file is created each time the COM Reader Module is started, providing a separate record for each operating session.
 

