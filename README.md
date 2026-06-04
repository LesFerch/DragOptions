# DragOptions

### Version 2.0.0

<img width="258" height="219" alt="image" src="https://github.com/user-attachments/assets/fc1a3280-258c-4b6e-a4b9-6e7559b53c61" />

This program loads to the System Tray and provides options that help prevent unwanted drag operations and/or ensure a double-click is registered. This is mostly of interest to users with high dpi displays.

**Note**: The app adds an HKCU registry entry to run itself on login, so that it's always in the System Tray. See the **Exit** option below for removal. 

## Change drag sensitivity

<img width="298" height="146" alt="image" src="https://github.com/user-attachments/assets/b6b82cbd-8896-4103-b7f0-987e4706dbb8" />

Select this option to set the mouse drag sensitivity. The change is immediate and permanent (per user) when you click **OK**. The value is the number of pixels you must move the cursor, with the button held down, before a drag operation starts.

On high density screens, the default value of 4 pixels is too little for most users and causes unwanted drag events. This is most problematic when you only want the right-click options to appear instead of the drag options. Similar issues occur with tablets and pen input. Increasing this value should prevent the unwanted drag events.

## Change double-click sensitivity

<img width="299" height="145" alt="image" src="https://github.com/user-attachments/assets/5ea1adb9-626e-48d1-a24e-047bd2e1cd34" />

Select this option to set the double-click sensitivity. The change is immediate and permanent (per user) when you click **OK**. The value is the number of pixels the cursor is allowed to move between first and second click and still be registered as a double-click. Increasing this value should make it easier to double-click on high dpi displays.

## Disable right-click drag

Select this option to disable right-click dragging altogether.

### Enable right-click drag

Select this option to enable right-click dragging. This option will do nothing if right-click dragging has not been disabled.

## Help

Select this option to open this web page. Right-click this web page and select **translate** to see it in your preferred language.

### Exit (Ctrl to Remove)

Select this option to close the app

**Note**: Hold down the **Ctrl** key when selecting **Exit** to also remove the registry entry that makes it run on login.

## Usage and memory impact

When left running as a system tray app, DragOptions uses around 5-6 MB RAM. It's only necessary to leave it running for the `Disable right-click drag` option. if you only use it to change drag sensitivity or double-click sensitivity, there's no need to leave it running. 

## How to Download and Install

[![image](https://github.com/LesFerch/WinSetView/assets/79026235/0188480f-ca53-45d5-b9ff-daafff32869e)Download the zip file](https://github.com/LesFerch/DragOptions/releases/download/2.0.0/DragOptions.zip)

**Note**: Some antivirus software may falsely detect the download as a virus. This can happen any time you download a new executable and may require extra steps to whitelist the file.

1. Download the zip file using the link above.
2. Extract **DragOptions.exe**.
3. Right-click **DragOptions.exe**, select Properties, check **Unblock**, and click **OK**.
4. Move **DragOptions.exe** (and optionally **Languages.ini**) to the folder of your choice.
5. Double-click **DragOptions.exe** to launch the app to the System Tray.
6. If you skipped step 3, then, in the SmartScreen window, click **More info** and then **Run anyway**.

**Note**: Some antivirus software may falsely detect the download as a virus. This can happen any time you download a new executable and may require extra steps to whitelist the file.

**Note**: If you move **DragOptions.exe** to a new location, just exit the app (if it's still in the System Tray). Then double-click the app to automatically update the startup registry entry.

## It's Multilingual!

Here's an example of DragOptions installed for German (de-DE):

<img width="335" height="225" alt="image" src="https://github.com/user-attachments/assets/7608d097-de3b-4f04-b9dc-1511d2b06411" />

The file **Language.ini** contains the app's text strings for all languages. It can be deleted if you only need English. Otherwise, keep it in the same folder as **DragOptions.exe**.

\
\
[![image](https://github.com/LesFerch/WinSetView/assets/79026235/63b7acbc-36ef-4578-b96a-d0b7ea0cba3a)](https://github.com/LesFerch/DragOptions)
