using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Minecraft Console Client")]
[assembly: AssemblyDescription("Lightweight console for Minecraft chat and automated scripts")]
//[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("https://github.com/ORelio/Minecraft-Console-Client")]
[assembly: AssemblyProduct("MinecraftClient")]
[assembly: AssemblyCopyright("Copyright © 2012-2019 ORelio & Contributors")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("78af6200-1f48-4daa-b473-109a9728b61f")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
// [assembly: AssemblyVersion("1.0.0")]
// [assembly: AssemblyFileVersion("1.0.0")]

// AppVeyor Build Number
// MCC will use the following command as CMD pre-build script in AppVeyor msbuild settings:
// echo [assembly: AssemblyConfiguration("AppVeyor build %APPVEYOR_BUILD_NUMBER%, built on %DATE% from commit %APPVEYOR_REPO_COMMIT:~0,7%")] >> MinecraftClient\Properties\AssemblyInfo.cs
// The command will add build info like the example below to be incorporated in Assembly Info in order to display build info on launch
// [assembly: AssemblyConfiguration("AppVeyor build 1234, built on Sun 31/12/2017 from commit abc1def")]
