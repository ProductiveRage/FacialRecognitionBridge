# Facial Detection Experiment (Bridge.NET port)

This is a port of my .NET [Face Detection](https://github.com/ProductiveRage/FacialRecognition) toy project to [Bridge.NET](http://bridge.net/), so that the code may run in the browser.

It includes a fraction of the [Accord.NET](https://github.com/accord-net/framework) library, also compiled with Bridge.NET (so that I have access to a support vector machine for classifying "possible face regions").

For more details about this project, refer to the [.NET version](https://github.com/ProductiveRage/FacialRecognition) or my blog post about it: "[Face or no face (finding faces in photos using C# and Accord.NET)](http://www.productiverage.com/face-or-no-face-finding-faces-in-photos-using-c-sharp-and-accordnet)".

To try the code out, clone the repo, perform a full build, set "Host" as the Startup Project and then hit F5 (if you don't built first then you'll just get a blank page since the the JavaScript won't have been generated and the browser won't be able to load the application logic). After a few seconds, you should see an image with the face outlined in green!

*(The code is considerably slower to run in the browser than the .NET version, it's something that I'd like to look  into - what, in particular, is slower and is it possible to do it differently in the browser, somehow, in order to make it faster)*