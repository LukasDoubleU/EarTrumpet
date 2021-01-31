This branch contains all changes from my 4 feature branches:

# Hide Apps
- Description: Added the option when right clicking an app to hide it via the 'garbage'-icon. The app will then be removed from all devices and will not show up on the next start. You can restore all hidden apps via the settings menu.
- Branch: hideApps
- Related Issue: https://github.com/File-New-Project/EarTrumpet/issues/558

# Persist App Volumes
- Description: This is a workaround for a Windows issue. When an executable is being updates its volume defaults back to 100. Presumably that is because the update changed the app's id, and windows uses the id to store the volume. EarTrumpet should memorize an apps volume by the exe's name and reapply that. Personally I witness this issue a lot with chrome and other chromium based browsers.
- Branch: persistVolume
- Issue: https://github.com/File-New-Project/EarTrumpet/issues/481
- How To Test:
    - Change the volume of an app to 1 via EarTrumpet
        - Only then will EarTrumpet persist that apps volume
    - Close EarTrumpet
    - Change the volume to 100 via default windows mixer
        - This is to simulate an exe resetting its volume
    - Start EarTrumpet
        - Previous behaviour: Apps volume will be 100
        - Now: Apps volume will go back to 1
- Risks: To tackle those this feature is optional and deactivated per default
    - Since the exe's volume is being persisted relative to its name, you can no longer assign different volumes to two different exe's that have the same name
    - Incompatible when there are other applications that modify app volumes

# Default App Volume
- Description: The volume of new apps is set to 100 by default. You should be able to configure an individual default volume that is then being applied to all new apps.
- Branch: defaultVolume
- Issue: https://github.com/File-New-Project/EarTrumpet/issues/329

# Change Communication Device
- Description: When swapping the default audio device it should be possible to optionally swap the communication device as well.
- Branch: changeComms
- Issue: https://github.com/File-New-Project/EarTrumpet/issues/348
	
	