# DragOptions

![image](https://github.com/LesFerch/DragOptions/assets/79026235/1e40cb3e-1c6b-427b-9bad-9513f98ed480)

## Set Mouse Drag Sensitivity and Optionally Disable Right-Click Drag

This program loads to the System Tray and provides options that help prevent unwanted drag operations.

**Note**: The app adds an HKCU registry entry to run itself on login, so that it's always in the System Tray. See the **Exit** option below for removal. 

### Change drag sensitivity

![image](https://github.com/LesFerch/DragOptions/assets/79026235/14aea680-baaa-4eaf-8895-bf6d96cfa45b)

Select this option to set the mouse drag sensitivity. The change is immediate and permanent (per user) when you click **OK**. The value is the number of pixels you must move the cursor, with the button held down, before a drag operation starts.

On high density screens, the default value of 4 pixels is too little for most users and causes unwanted drag events. This is most problematic when you only want the right-click options to appear instead of the drag options. Similar issues occur with tablets and pen input. Increasing this value should prevent the unwanted drag events.

### Disable right-click drag

Select this option to disable right-click dragging altogether.

### Enable right-click drag

Select this option to enable right-click dragging. This option will do nothing if right-click dragging has not been disabled.

### Help

Select this option to open this web page. Right-click this web page and select **translate** to see it in your preferred language.

### Exit (Ctrl to Remove)

Select this option to close the app

**Note**: Hold down the **Ctrl** key when selecting **Exit** to also remove the registry entry that makes it run on login.

## How to Download and Install

[![image](https://github.com/LesFerch/WinSetView/assets/79026235/0188480f-ca53-45d5-b9ff-daafff32869e)Download the zip file](https://github.com/LesFerch/DragOptions/releases/download/1.0.1/DragOptions.zip)

**Note**: Some antivirus software may falsely detect the download as a virus. This can happen any time you download a new executable and may require extra steps to whitelist the file.

1. Download the zip file using the link above.
2. Extract **DragOptions.exe**.
3. Right-click **DragOptions.exe**, select Properties, check **Unblock**, and click **OK**.
4. Move **DragOptions.exe** (and optionally **Languages.ini**) to the folder of your choice.
5. Double-click **DragOptions.exe** to launch the app to the System Tray.
6. If you skipped step 3, then, in the SmartScreen window, click **More info** and then **Run anyway**.

**Note**: If you move **DragOptions.exe** to a new location, just exit the app (if it's still in the System Tray). Then double-click the app to automatically update the startup registry entry.

## It's Multilingual!

Here's an example of DragOptions installed for German (de-DE):

![image](https://github.com/LesFerch/DragOptions/assets/79026235/aef2a434-9e77-4ace-a544-0035fc425c82)

The file **Language.ini** contains the app's text strings for all languages except English. It can be deleted if you only need English. Otherwise, keep it in the same folder as **DragOptions.exe**.

\
\
[![image](https://github.com/LesFerch/WinSetView/assets/79026235/63b7acbc-36ef-4578-b96a-d0b7ea0cba3a)](https://github.com/LesFerch/DragOptions)
