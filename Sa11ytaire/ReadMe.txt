
*** The Sa11ytaire Experiment ***

The Sa11ytaire Experiment is a C# UWP XAML app for Windows which explores 
various aspects of how a solitaire game app might be made more accessible.

Part 1 of the experiment focuses on how a variety of input and output methods 
available in Windows might be leveraged by the app. This includes raising custom 
notifications for screen readers, using the Windows 10 eye control feature, 
using an Xbox Adaptive Controller, or using Windows Speech Recognition. 
Demo videos of the various input and output methods in use, are at 
https://www.linkedin.com/pulse/sa11ytaire-experiment-end-part-1-guy-barker.

Part 2 of the experiment focuses on how Azure Cognitive Services might 
be used by the app to provide exciting new experiences in the game. 
This includes using custom speech recognition with language understanding, 
custom image recognition to identify physical playing cards presented to the 
game, and the incorporation of an FAQ bot with a friendly chit-chat personality.

Important: This app does not demonstrate best practices around designing and 
coding apps. For example, the app's MainPage class is full of feature-specific code 
which should instead be encapsulated in feature-specific classes. Also the XAML used 
to create the app's UI should make heavy use of templates to avoid code duplication.

Despite there being plenty of opportunities to improve the quality of the app's code, 
it's been shared now as-is, for anyone who would find the accessibility-related 
action taken by the code, or the interaction with Azure Cognitive Services, helpful 
as they build their own accessible solutions.

All code in the Sa11ytaire is available under MIT License, as described in the License.txt file.

To build the app, open the Visual Studio solution file, Sol4All.sln. You may be asked to 
turn on Developer mode when opening the solution file. You may also be asked to update 
the project to a later version of the platform SDK, depending on what versions of the SDK 
are already installed on your device.

When the app is run, the game of Sa11ytaire will respond to many types of input. However, to 
leverage Azure Cognitive Services, functional endpoints and keys must be added to the app. 
The related files to be updated are in the "AzureCognitiveServices" folder. Similarly valid
connection data would have to be supplied in order to interact with a bot.

The action taken in the code in response to calling the Speech to Text service simply reflects 
whatever test was most recently performed with the app.
