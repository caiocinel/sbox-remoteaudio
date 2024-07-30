using MP3Sharp;
using Sandbox.Utility;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace cailu.Radinho;

public class Radinho
{    
	public GameObject localMesh;
	public SoundHandle music;

	public float musicVolume;


	Radinho(float volume)
	{
		musicVolume = volume;
	}

    [ConCmd( "playtrack", Help = "URL of audio. Must be in quotes." )]
    internal static async void PlayTrack( string audioUrl, float volume = 1 )
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


			await new Radinho(volume).LoadMusicFromWeb( response );
        }
        catch ( Exception e )
        {
            Log.Warning( e );
            return;
        }		
    }


	public async Task LoadMusicFromWeb( HttpResponseMessage url )
    {

		var result = await url.Content.ReadAsByteArrayAsync();

		var mp3 = new MP3Stream(new MemoryStream(result));

		var stream = new SoundStream( sampleRate: 48000, channels: 2 );

		var buffer = new byte[8192];
		
		var delay = 1000 * buffer.Length / sizeof( short ) / 2 / 48000;
		int bytesRead = -1;
		int bytesTotal = 0;

		while ( (music == null || !music.Finished) && bytesRead != 0 )
		{
			CameraComponent Camera = Game.ActiveScene.GetAllComponents<CameraComponent>().FirstOrDefault();

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
					music = stream.Play();
					music.Position = Camera.Transform.Position;
					music.Occlusion = false;
					music.Volume = musicVolume;

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
				music.Position = localMesh.Transform.Position;
		}

		music.Stop();
		mp3.Close();
		stream.Dispose();		
		localMesh?.Destroy();		
	}

}
