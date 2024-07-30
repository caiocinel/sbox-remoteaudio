using MP3Sharp;
using Sandbox.Utility;
using System;
using System.IO;
using System.Threading.Tasks;

namespace cailu.Radinho;

public class Radinho
{    
	public static GameObject LocalMesh;









    [ConCmd( "playtrack", Help = "URL of audio. Must be in quotes." )]
    internal static async void SetImage( string audioUrl )
    {
        try
        {
            var uri = new Uri( audioUrl );
            var response = await Http.RequestAsync( uri.AbsoluteUri );
            
            if ( !response.Content.Headers.GetValues( "Content-Type" ).Any( type => type.Contains( "audio" )))
            {
                Log.Error( response.Content);
                throw new FileNotFoundException("Not an audio type");
            }

            await LoadMusicFromWeb( uri.AbsoluteUri );
        }
        catch ( Exception e )
        {
            Log.Warning( e );
            return;
        }		
    }



	public static void Place()
	{

		GameTransform local = Game.ActiveScene.Camera.Transform;

		var config = new CloneConfig
		{
			Name = $"Radinho - {Steam.PersonaName}",
			Transform = local.World,
			PrefabVariables = new Dictionary<string, object>
			{
				{ "Owner", Steam.PersonaName },
			}
		};



		LocalMesh?.Destroy();

		LocalMesh = GameObject.Clone( "prefabs/radinho.prefab", config );

		LocalMesh.NetworkSpawn();
		LocalMesh.SetPrefabSource( "prefabs/radinho.prefab" );
		return;
	}


	public static async Task LoadMusicFromWeb( string url )
    {

		var result = await Http.RequestBytesAsync( url );

		var mp3 = new MP3Stream(new MemoryStream(result));

		var stream = new SoundStream( sampleRate: 48000, channels: 2 );

		var buffer = new byte[8192];
		
		var delay = 1000 * buffer.Length / sizeof( short ) / 2 / 48000;
		int bytesRead = -1;
		int bytesTotal = 0;
		SoundHandle music = null;

		while ( (music == null || !music.Finished) && bytesRead != 0 )
		{
			if ( stream.QueuedSampleCount < buffer.Length * 2 )
			{
				bytesRead = mp3.Read( buffer, 0, buffer.Length );
				var data = new short[bytesRead / 2];
				for ( int i = 0; i < data.Length; i++ )
				{
					data[i] = (short)(buffer[i * 2] | (buffer[i * 2 + 1] << 8));
				}

				stream.WriteData( new Span<short>( data ) );
				if ( music == null )
				{
					music = stream.Play( decibels: 30 );
					CameraComponent Camera = Game.ActiveScene.GetAllComponents<CameraComponent>().FirstOrDefault();
					music.Position = Camera.Transform.Position;
					Place();
				}
				bytesTotal += bytesRead;
				
			}
			else			
				await GameTask.DelayRealtime( delay );			
		}

		music.Stop();
		mp3.Close();
		stream.Dispose();
		//LocalMesh?.Destroy();		
	}

}
