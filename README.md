# App-Muter-mk2
This application was made for the Titanfall 2 speedrun community in order to mute the audio of external applications while running in order to charge a grenade and hear whenever you would need to throw it  
Currently the application only accepts mouse input functionality, but will implement keyboard inputs if someone wants it

# Installation
Install the latest release build, make sure to keep it in a solo directory as it will make a settings.xml file upon launch

# How it works
After you set a bind and choose an application, whenever you press and hold that bind the application will be set to the target volume which by default is set to 0  
As soon as you release the bind the application will return to whatever volume it was at previously

# Setting a Bind
Locate the bind button area and click it with your left mouse button then once the button text reads 'Press Any Button', wait for a small time then
1. Press the mouse button you wish to set the bind to
2. Hold the mouse button down for a small time
3. Release the mouse button

# Choosing an Application
Locate the application dropdown and click the dropdown arrow. This should then show every currently running application with an active window in the drop down menu  
Then just select which application you want to change the volume of when pressing the bind

# Setting a Target Volume (optional)
Locate the volume textbox, this box will only accept a numeric input. If left at zero, the application will be fully muted when the bind is pressed  
If you wish to have the application only lowered to a specific volume then you can input the target volume you wish to set the app to when the bind is pressed  
The number you input is the whole number percentage that you will find if you use your volume mixer (100 max, 0 min)

# Enable & Disable
Locate the [Enable] and [Disable] buttons at the top of the application window. To toggle the volume change functionality, just select the option that corresponds to the state you wish the application to be at
