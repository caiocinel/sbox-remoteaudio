using MP3Sharp;
using System;
using System.Threading.Tasks;

namespace Sandbox.Library.RemoteAudio
{
	public class RemoteAudio
	{
		public SoundHandle handle;

		public Action<SoundHandle> OnEnabled;
		public Action<SoundHandle> OnUpdate;
		public Action OnDestroy;

		public async Task Play( string url )
		{
			var response = await Http.RequestAsync( url );

			if ( !response.Content.Headers.GetValues( "Content-Type" ).Any( type => type.Contains( "audio" ) ) )
				throw new Exception( "Invalid Content-Type" );

			var mp3 = new MP3Stream( await response.Content.ReadAsStreamAsync() );

			var stream = new SoundStream( sampleRate: 48000, channels: 2 );

			var buffer = new byte[8192];

			var delay = 1000 * buffer.Length / sizeof( short ) / 2 / 48000;
			int bytesRead = -1;
			int bytesTotal = 0;

			while ( (handle == null || !handle.Finished) && bytesRead != 0 )
			{			
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
						OnEnabled?.Invoke( this.handle );

					}
					bytesTotal += bytesRead;

				}
				else
					await GameTask.DelayRealtime( delay );

				OnUpdate?.Invoke( this.handle );
			}

			handle.Stop();
			handle.Dispose();
			mp3.Dispose();
			stream.Dispose();
			OnDestroy?.Invoke();
		}

	}
}
