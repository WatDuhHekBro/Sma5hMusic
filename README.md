## Sma5hMusic GUI - What is it?
Sma5h.CLI and Sma5hMusic are a series of tools to import additional tracks to Smash Ultimate.
This tool is highly experimental and may not always work as expected.
* For detailed setup steps: https://github.com/Deinonychus71/Sma5hMusic/wiki
* **Always keep backups of your files and savegames.**
* **This mod is not safe online!**
* Suggestions are welcome. Please create an issue for it.

## Linux Notes
- For all `.csproj` files that require it, [preventing weird reference issues](https://stackoverflow.com/a/65161916): `<GenerateErrorForMissingTargetingPacks>false</GenerateErrorForMissingTargetingPacks>`
- To build the program: `dotnet publish -c Release`
- To run the program: `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 DOTNET_ROOT=<path>/dotnet-sdk-3.1.426-linux-x64/ ./Sma5hMusic.GUI`
- [3.1 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/3.1)
- [You need Globalization Invariant because](https://stackoverflow.com/a/64370938): `Couldn't find a valid ICU package installed on the system.`
- If you get `No usable version of libssl was found`, then you need to have some version of OpenSSL 1.x installed on your system, as older .NET versions don't support OpenSSL 3.x.

## Thanks & Repos of the different tools
1.  Research: soneek
2.  Testing: Demonslayerx8, Segtendo
3.  Icon: Segtendo
4.  prcEditor: https://github.com/BenHall-7/paracobNET - BenHall-7
5.  paramLabels: https://github.com/ultimate-research/param-labels - BenHall-7, jam1garner, Dr-HyperCake, Birdwards, ThatNintendoNerd, ScanMountGoat, Meshima, Blazingflare, TheSmartKid, jugeeya, Demonslayerx8
6.  msbtEditor: https://github.com/IcySon55/3DLandMSBTeditor - IcySon55, exelix11
7.  nus3audio: https://github.com/jam1garner/nus3audio-rs - jam1garner
8.  bgm-property: https://github.com/jam1garner/smash-bgm-property - jam1garner
9.  VGAudio: https://github.com/Thealexbarney/VGAudio - Thealexbarney, soneek, jam1garner, devlead, Raytwo, nnn1590
10.  vgmstream: https://github.com/vgmstream/vgmstream - bnnm, kode54, NicknineTheEagle, bxaimc, Thealexbarney
All contributors: https://github.com/vgmstream/vgmstream/graphs/contributors
11. CrossArc: https://github.com/Ploaj/ArcCross Ploaj, ScanMountGoat, BenHall-7, shadowninja108, jam1garner, M-1-RLG

## How to create an issue - bug ##
1. Please do not create an issue if you're having trouble with the setup. I will not provide the resource files as this would be piracy. You need to extract them yourself from your own backup.
2. Please check the wiki for troubleshooting, it may contain an answer to your question already. I will try to keep it updated.
3. At the very least you should make sure you are using latest with unmodified files and no other mods enabled as first troubleshooting steps.
4. I have limited time so issues such as 'it's not working please help' will be ignored / closed. 
5. Please provide the following :
- Version detected by the program (should be software latest and game latest)
- Provide a sample of the log where the issue is found or a screenshot
- Provide reproducible steps. (such as "I launched the software, did this, clicked there, and then this happened")
- If relevant please provide the metadata_mod.json files or override json files (from /Mods) that you think might have an issue. If you're having issues to start the program you can link the appsettings.json (Root folder) file too.

## How to create an issue - enhancements ##
* Enhancement requests are welcome! But it may take a while for me to answer back on them.
