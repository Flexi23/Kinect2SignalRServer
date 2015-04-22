# Kinect 2 SignalR Server

The Microsoft Visual Studio solution to broadcast Kinect 2.0 body frames with a Websocket server. This is a mash-up of the "Body Basics-WPF" sample from the Kinect SDK 2.0 in C# and the SignalR chat hub solution from the download link under  https://code.msdn.microsoft.com/SignalR-Getting-Started-b9d18aa9.

These are the lines that make up the contract for the SignalR:

```
	public class KinectHub : Hub
	{
		public void OnBody(string bodyJson, string projectionMappedPointsJson)
		{
			Clients.All.onBody(bodyJson, projectionMappedPointsJson);
		}

		public void OnBodies(string trackedBodyTrackingIdsJson, long frame)
		{
			Clients.All.onBodies(trackedBodyTrackingIdsJson, frame);
		}
	}
```
