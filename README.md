## Setup

1. Clone recursively: `git clone --recursive https://github.com/AntonC9018/CryptoTrades`;
2. Install .NET 7, Unity 2022.2.7f1 and either Visual Studio or Rider;
3. Select either Visual Studio or Rider as the default code editor in the Unity editor.
   Change it in Project Settings > External Tools.
   Either one of these is required to be able to generate the project files.
4. Run the command `build setup` in the root of the project (or `nuke setup` if nuke is installed globally).
5. Open `CryptoTrades.sln` from the root folder in your IDE (if working on multiple projects) or the unity-generated `CryptoTrades/CryptoTrades.sln` (if working on the Unity project only).

> Note: the Unity project folder is one level deep
> Your IDE might not recognize that the project is a Unity project, unless the sln is in the root of the Unity project.
> As much as it hurts, working on the Unity project in a separate window is more viable, since the IDE helps you way more that way.

You may also need to regenerate the UXML schema definition files, I haven't tested if they're created automatically.
You need to do `Assets > Update UXML Schema` in the Unity editor.
See [docs](https://docs.unity3d.com/Manual/UIE-WritingUXMLTemplate.html), the Schema Definition section, for more info. 
