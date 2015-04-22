using Owin;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Cors;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Kinect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;

namespace SignalRSelfHost
{
	class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			app.UseCors(CorsOptions.AllowAll);
			app.MapSignalR();
		}
	}

	class Program
	{
		static private string url = "http://localhost:8080";
		static private KinectSensor kinectSensor = null;
		static private CoordinateMapper coordinateMapper = null;
		static private BodyFrameReader bodyFrameReader = null;
		static private Body[] bodies = null;

		private const float InferredZPositionClamp = 0.1f;

		static void Main(string[] args)
		{
			kinectSensor = KinectSensor.GetDefault();
			coordinateMapper = kinectSensor.CoordinateMapper;
			bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
			kinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

			Console.WriteLine("Open Kinect");

			kinectSensor.Open();

			using (WebApp.Start(url))
			{
				Console.WriteLine("Websocket server started: {0}", url);

				if (bodyFrameReader != null)
				{
					bodyFrameReader.FrameArrived += Reader_FrameArrived;
				}

				ConnectKinectClient();

				Console.WriteLine("Press [Enter] to stop the server.");
				Console.ReadLine();

				if (bodyFrameReader != null)
				{
					bodyFrameReader.Dispose();
					bodyFrameReader = null;
				}

				if (kinectSensor != null)
				{
					kinectSensor.Close();
					kinectSensor = null;
				}
			}
		}

		private static IHubProxy kinectHubProxy = null;
		private static bool kinectClientConnected = false;

		private static async Task ConnectKinectClient()
		{
			HubConnection selfConnection = new HubConnection(url);

			kinectHubProxy = selfConnection.CreateHubProxy(typeof(KinectHub).Name);

			await selfConnection.Start().ContinueWith(task =>
			{
				if (task.IsFaulted)
				{
					Console.WriteLine("Kinect client connection failed: {0}", task.Exception.GetBaseException());
				}
				else
				{
					Console.WriteLine("Kinect client connected");
					kinectClientConnected = true;
				}
			});
		}

		static private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
		{
			Console.WriteLine(kinectSensor.IsAvailable ? "Kinect Sensor up and running" : "Kinect Sensor not available");
		}
		static private long frame = 0;
		static private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
		{
			bool dataReceived = false;

			using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
			{
				if (bodyFrame != null)
				{
					if (bodies == null)
					{
						bodies = new Body[bodyFrame.BodyCount];
					}
					bodyFrame.GetAndRefreshBodyData(bodies);
					dataReceived = true;
					frame++;
				}
			}

			if (dataReceived)
			{
				foreach (Body body in bodies)
				{
					IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

					Dictionary<JointType, Array> jointPoints = new Dictionary<JointType, Array>();

					foreach (JointType jointType in joints.Keys)
					{
						CameraSpacePoint position = joints[jointType].Position;
						if (position.Z < 0)
						{
							position.Z = InferredZPositionClamp;
						}

						DepthSpacePoint depthSpacePoint = coordinateMapper.MapCameraPointToDepthSpace(position);
						jointPoints[jointType] = new float[] { depthSpacePoint.X, depthSpacePoint.Y };
					}

					if (kinectClientConnected)
					{
						var bodyJson = JsonConvert.SerializeObject(body);
						var projectionMappedPointsJson = "";
						if (body.IsTracked)
						{
							projectionMappedPointsJson = JsonConvert.SerializeObject(jointPoints);
							try
							{
								kinectHubProxy.Invoke("OnBody", bodyJson, projectionMappedPointsJson);
							}
							catch (Exception x)
							{
								Console.WriteLine(x.GetType().Name + ": " + x.Message);
							}
						}
					}
				}
				var trackedBodyTrackingIdsJson = JsonConvert.SerializeObject(bodies.Where(b => b.IsTracked).Select(b => b.TrackingId));
				try
				{
					kinectHubProxy.Invoke("OnBodies", trackedBodyTrackingIdsJson, frame);
				}
				catch (Exception x)
				{
					Console.WriteLine(x.GetType().Name + ": " + x.Message);
					Console.WriteLine("reconnect");
					ConnectKinectClient();
				}
			}
		}
	}

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
}