using MP3Sharp;
using Sandbox.Utility;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sandbox.Library.RemoteAudio
{
	public class RemoteAudio
	{

		
		public GameObject localMesh;
		public SoundHandle handle;
		public float volume;
		public Transform transform;


		public void OnEnabled()
		{
			Log.Info( "OnEnable" );
		}

		public void OnUpdate()
		{
			Log.Info( "OnUpdate" );
		}

		public RemoteAudio( float pVolume )
		{
			volume = pVolume;
		}

		public async Task LoadMusicFromWeb( HttpResponseMessage url )
		{

			var result = await url.Content.ReadAsByteArrayAsync();

			var mp3 = new MP3Stream( new MemoryStream( result ) );

			var stream = new SoundStream( sampleRate: 48000, channels: 2 );

			var buffer = new byte[8192];

			var delay = 1000 * buffer.Length / sizeof( short ) / 2 / 48000;
			int bytesRead = -1;
			int bytesTotal = 0;

			while ( (handle == null || !handle.Finished) && bytesRead != 0 )
			{
				CameraComponent Camera = Game.ActiveScene.GetAllComponents<CameraComponent>().FirstOrDefault();

				if ( stream.QueuedSampleCount < buffer.Length * 2 )
				{
					bytesRead = mp3.Read( buffer, 0, buffer.Length );
					var data = new short[bytesRead / 2];
					for ( int i = 0; i < data.Length; i++ )
					{
						data[i] = (short)(buffer[i * 2] | buffer[i * 2 + 1] << 8);
					}

					stream.WriteData( new Span<short>( data ) );
					if ( handle == null )
					{
						handle = stream.Play();
						handle.Position = Camera.Transform.Position;
						handle.Occlusion = false;
						handle.Volume = volume;

						var config = new CloneConfig
						{
							Name = $"Radinho - {Steam.PersonaName}",
							Transform = Camera.Transform.World,
							PrefabVariables = new Dictionary<string, object>
						{
							{ "Owner", Steam.PersonaName },
						}
						};

						localMesh = GameObject.Clone( "prefabs/radinho.prefab", config );
						localMesh.Tags.Add( "noblockaudio" );
						localMesh.NetworkSpawn();


						localMesh.SetPrefabSource( "prefabs/radinho.prefab" );
					}
					bytesTotal += bytesRead;

				}
				else
					await GameTask.DelayRealtime( delay );

				if ( localMesh != null )
					handle.Position = localMesh.Transform.Position;

				OnUpdate();
			}

			handle.Stop();
			mp3.Close();
			stream.Dispose();
			localMesh?.Destroy();
		}

	}
}
